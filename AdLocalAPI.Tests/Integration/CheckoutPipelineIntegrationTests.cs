using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.Constants;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Integration
{
    public class CheckoutPipelineIntegrationTests
    {
        private (CheckoutService service, Mock<IPedidoRepository> repoMock, Mock<INotificacionService> notifMock) CrearServicio(long userId = 5)
        {
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim("id", userId.ToString()),
                new Claim("rol", RolesUsuario.Cliente),
                new Claim(ClaimTypes.Role, RolesUsuario.Cliente)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            var jwt = new JwtContext(accessor);
            var repoMock = new Mock<IPedidoRepository>();
            var notifMock = new Mock<INotificacionService>();

            var service = new CheckoutService(
                repoMock.Object,
                jwt,
                notifMock.Object,
                NullLogger<CheckoutService>.Instance
            );

            return (service, repoMock, notifMock);
        }

        private CheckoutCarritoItemDto CrearItemCarrito(
            long detalleId,
            long prodId,
            string prodNombre,
            decimal precio,
            int cantidad,
            long comercioId,
            Guid comercioUuid,
            string comercioNombre,
            bool manejaStock = true,
            int stock = 10)
        {
            var comercio = new Comercio
            {
                Id = comercioId,
                Uuid = comercioUuid,
                Nombre = comercioNombre,
                Activo = true
            };

            var producto = new ProductosServicios
            {
                Id = prodId,
                Nombre = prodNombre,
                Precio = precio,
                IdComercio = comercioId,
                Activo = true,
                Eliminado = false,
                Visible = true,
                Disponible = true,
                Modalidad = ModalidadProductoServicio.Compra,
                PermiteRecoger = true,
                PermiteDomicilio = true,
                ManejaStock = manejaStock,
                Stock = stock
            };

            return new CheckoutCarritoItemDto
            {
                IdDetalleCarrito = detalleId,
                IdProductoServicio = prodId,
                Cantidad = cantidad,
                Comercio = comercio,
                Producto = producto
            };
        }

        // ==============================================================
        // 1. VALIDACIONES DE CARRITO Y CONFIGURACIÓN
        // ==============================================================

        [Fact]
        public async Task Confirmar_CarritoVacio_Retorna400()
        {
            var (service, repoMock, _) = CrearServicio(5);

            repoMock.Setup(r => r.ObtenerUsuarioAsync(5)).ReturnsAsync(new Usuario { Id = 5, Nombre = "Juan" });
            repoMock.Setup(r => r.ObtenerCarritoActivoAsync(5)).ReturnsAsync(new Carrito { Id = 10, IdUsuario = 5, Activo = true });
            repoMock.Setup(r => r.ObtenerProductosCarritoAsync(10)).ReturnsAsync(new List<CheckoutCarritoItemDto>());

            var resultado = await service.Confirmar(new ConfirmarCheckoutDto());

            Assert.Equal("400", resultado.Codigo);
            Assert.Equal("Tu carrito está vacío.", resultado.Mensaje);
        }

        [Fact]
        public async Task Confirmar_FaltaConfigurarComercio_Retorna400()
        {
            var (service, repoMock, _) = CrearServicio(5);
            var comercioUuid = Guid.NewGuid();

            repoMock.Setup(r => r.ObtenerUsuarioAsync(5)).ReturnsAsync(new Usuario { Id = 5, Nombre = "Juan" });
            repoMock.Setup(r => r.ObtenerCarritoActivoAsync(5)).ReturnsAsync(new Carrito { Id = 10, IdUsuario = 5, Activo = true });
            repoMock.Setup(r => r.ObtenerProductosCarritoAsync(10)).ReturnsAsync(new List<CheckoutCarritoItemDto>
            {
                CrearItemCarrito(1, 50, "Pizza Grande", 150m, 2, 1, comercioUuid, "Pizzería Test")
            });

            // Enviar DTO sin comercios
            var resultado = await service.Confirmar(new ConfirmarCheckoutDto
            {
                Comercios = new List<CheckoutComercioDto>() // Lista vacía
            });

            Assert.Equal("400", resultado.Codigo);
            Assert.Contains("Debes configurar el pago y entrega", resultado.Mensaje);
        }

        // ==============================================================
        // 2. CONTROL DE IDEMPOTENCIA Y CONCURRENCIA
        // ==============================================================

        [Fact]
        public async Task Confirmar_IdempotenciaCompletada_RetornaRespuestaPreviaCacheada()
        {
            var (service, repoMock, _) = CrearServicio(5);
            var key = "idemp_test_key_123";

            var responseOriginal = new ConfirmarCheckoutResponseDto
            {
                TotalPedidos = 1,
                TotalGeneral = 300m,
                Pedidos = new List<PedidoCheckoutResponseDto>
                {
                    new() { NumeroPedido = "ORD-0001", Total = 300m }
                }
            };
            var json = JsonSerializer.Serialize(responseOriginal);

            repoMock.Setup(r => r.ObtenerUsuarioAsync(5)).ReturnsAsync(new Usuario { Id = 5, Nombre = "Juan" });
            repoMock.Setup(r => r.ObtenerCarritoActivoAsync(5)).ReturnsAsync(new Carrito { Id = 10, IdUsuario = 5, Activo = true });
            repoMock.Setup(r => r.ObtenerProductosCarritoAsync(10)).ReturnsAsync(new List<CheckoutCarritoItemDto>
            {
                CrearItemCarrito(1, 10, "Item", 300m, 1, 1, Guid.NewGuid(), "Tienda")
            });

            repoMock.Setup(r => r.ObtenerIdempotenciaAsync(5, key)).ReturnsAsync(new CheckoutIdempotencia
            {
                IdUsuario = 5,
                IdempotencyKey = key,
                Status = "completed",
                ResponseJson = json
            });

            var resultado = await service.Confirmar(new ConfirmarCheckoutDto { IdempotencyKey = key });

            Assert.Equal("200", resultado.Codigo);
            Assert.Equal("Pedido previamente confirmado (idempotente).", resultado.Mensaje);
            Assert.NotNull(resultado.Respuesta);
            Assert.Equal(300m, resultado.Respuesta.TotalGeneral);
            // Verificar que no se volvió a invocar la persistencia del pedido
            repoMock.Verify(r => r.GuardarCheckoutAsync(
                It.IsAny<List<Pedido>>(),
                It.IsAny<List<(long, int, string)>>(),
                It.IsAny<Carrito>(),
                It.IsAny<CheckoutIdempotencia?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()
            ), Times.Never);
        }

        [Fact]
        public async Task Confirmar_IdempotenciaEnProcesamiento_Retorna409Conflict()
        {
            var (service, repoMock, _) = CrearServicio(5);
            var key = "idemp_in_progress";

            repoMock.Setup(r => r.ObtenerUsuarioAsync(5)).ReturnsAsync(new Usuario { Id = 5, Nombre = "Juan" });
            repoMock.Setup(r => r.ObtenerCarritoActivoAsync(5)).ReturnsAsync(new Carrito { Id = 10, IdUsuario = 5, Activo = true });
            repoMock.Setup(r => r.ObtenerProductosCarritoAsync(10)).ReturnsAsync(new List<CheckoutCarritoItemDto>
            {
                CrearItemCarrito(1, 10, "Item", 100m, 1, 1, Guid.NewGuid(), "Tienda")
            });

            repoMock.Setup(r => r.ObtenerIdempotenciaAsync(5, key)).ReturnsAsync(new CheckoutIdempotencia
            {
                IdUsuario = 5,
                IdempotencyKey = key,
                Status = "processing"
            });

            var resultado = await service.Confirmar(new ConfirmarCheckoutDto { IdempotencyKey = key });

            Assert.Equal("409", resultado.Codigo);
            Assert.Contains("ya se encuentra en procesamiento", resultado.Mensaje);
        }

        // ==============================================================
        // 3. FLUJO EXITOSO DE CONFIRMACIÓN Y DESCUENTO DE STOCK
        // ==============================================================

        [Fact]
        public async Task Confirmar_FlujoExitoso_GuardaPedidoYNotificaComercio()
        {
            var (service, repoMock, notifMock) = CrearServicio(5);
            var comercioUuid = Guid.NewGuid();

            repoMock.Setup(r => r.ObtenerUsuarioAsync(5)).ReturnsAsync(new Usuario { Id = 5, Nombre = "Juan Pérez" });
            repoMock.Setup(r => r.ObtenerCarritoActivoAsync(5)).ReturnsAsync(new Carrito { Id = 10, IdUsuario = 5, Activo = true });
            repoMock.Setup(r => r.ObtenerProductosCarritoAsync(10)).ReturnsAsync(new List<CheckoutCarritoItemDto>
            {
                CrearItemCarrito(101, 55, "Café Americano", 45m, 2, 1, comercioUuid, "Café Central", manejaStock: true, stock: 10)
            });

            repoMock.Setup(r => r.ObtenerIdempotenciaAsync(5, It.IsAny<string>())).ReturnsAsync((CheckoutIdempotencia?)null);
            repoMock.Setup(r => r.RegistrarIdempotenciaInicioAsync(It.IsAny<CheckoutIdempotencia>())).Returns(Task.CompletedTask);

            repoMock.Setup(r => r.ObtenerConfiguracionPagoAsync(1)).ReturnsAsync(new ConfiguracionPagoComercio
            {
                IdComercio = 1,
                AceptaEfectivo = true,
                AceptaTransferencia = false
            });

            repoMock.Setup(r => r.ObtenerConfiguracionAsync(It.IsAny<string>())).ReturnsAsync((ConfiguracionSistema?)null);

            List<Pedido>? pedidosGuardados = null;
            List<(long Id, int Cantidad, string Nombre)>? stockActualizado = null;

            repoMock.Setup(r => r.GuardarCheckoutAsync(
                It.IsAny<List<Pedido>>(),
                It.IsAny<List<(long, int, string)>>(),
                It.IsAny<Carrito>(),
                It.IsAny<CheckoutIdempotencia?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()
            ))
            .Callback<List<Pedido>, List<(long, int, string)>, Carrito, CheckoutIdempotencia?, string?, CancellationToken>(
                (p, s, c, idemp, json, token) =>
                {
                    pedidosGuardados = p;
                    stockActualizado = s;
                }
            )
            .Returns(Task.CompletedTask);

            var resultado = await service.Confirmar(new ConfirmarCheckoutDto
            {
                IdempotencyKey = "key_exitosa_1",
                Comercios = new List<CheckoutComercioDto>
                {
                    new()
                    {
                        ComercioUuid = comercioUuid,
                        MetodoPago = MetodoPagoPedido.Efectivo,
                        TipoEntrega = TipoEntregaPedido.Recoger
                    }
                }
            });

            Assert.Equal("200", resultado.Codigo);
            Assert.NotNull(pedidosGuardados);
            Assert.Single(pedidosGuardados);
            Assert.Equal(90m, pedidosGuardados[0].Total); // 2 x $45
            Assert.NotNull(stockActualizado);
            Assert.Single(stockActualizado);
            Assert.Equal(55, stockActualizado[0].Id);
            Assert.Equal(2, stockActualizado[0].Cantidad);

            // Verificar desacoplamiento de notificaciones
            notifMock.Verify(n => n.NotificarComercioAsync(
                It.IsAny<Pedido>(),
                TipoNotificacionPedido.PedidoCreado,
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Once);
        }

        // ==============================================================
        // 4. ATOMICIDAD ANTE FALLO DE STOCK CONCURRENTE
        // ==============================================================

        [Fact]
        public async Task Confirmar_FalloDeStockEnBaseDeDatos_Retorna400YNoCompletaPedido()
        {
            var (service, repoMock, _) = CrearServicio(5);
            var comercioUuid = Guid.NewGuid();

            repoMock.Setup(r => r.ObtenerUsuarioAsync(5)).ReturnsAsync(new Usuario { Id = 5, Nombre = "Juan Pérez" });
            repoMock.Setup(r => r.ObtenerCarritoActivoAsync(5)).ReturnsAsync(new Carrito { Id = 10, IdUsuario = 5, Activo = true });
            repoMock.Setup(r => r.ObtenerProductosCarritoAsync(10)).ReturnsAsync(new List<CheckoutCarritoItemDto>
            {
                CrearItemCarrito(102, 70, "Pastel Chocolate", 200m, 5, 1, comercioUuid, "Panadería", manejaStock: true, stock: 10)
            });

            repoMock.Setup(r => r.ObtenerIdempotenciaAsync(5, It.IsAny<string>())).ReturnsAsync((CheckoutIdempotencia?)null);
            repoMock.Setup(r => r.RegistrarIdempotenciaInicioAsync(It.IsAny<CheckoutIdempotencia>())).Returns(Task.CompletedTask);

            repoMock.Setup(r => r.ObtenerConfiguracionPagoAsync(1)).ReturnsAsync(new ConfiguracionPagoComercio
            {
                IdComercio = 1,
                AceptaEfectivo = true
            });

            repoMock.Setup(r => r.ObtenerConfiguracionAsync(It.IsAny<string>())).ReturnsAsync((ConfiguracionSistema?)null);

            // Simular fallo atómico de stock insuficiente en BD
            repoMock.Setup(r => r.GuardarCheckoutAsync(
                It.IsAny<List<Pedido>>(),
                It.IsAny<List<(long, int, string)>>(),
                It.IsAny<Carrito>(),
                It.IsAny<CheckoutIdempotencia?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()
            ))
            .ThrowsAsync(new InvalidOperationException("Stock insuficiente para el producto 'Pastel Chocolate'. Por favor actualiza tu carrito."));

            var resultado = await service.Confirmar(new ConfirmarCheckoutDto
            {
                IdempotencyKey = "key_stock_fail",
                Comercios = new List<CheckoutComercioDto>
                {
                    new()
                    {
                        ComercioUuid = comercioUuid,
                        MetodoPago = MetodoPagoPedido.Efectivo,
                        TipoEntrega = TipoEntregaPedido.Recoger
                    }
                }
            });

            Assert.Equal("400", resultado.Codigo);
            Assert.Contains("Stock insuficiente para el producto 'Pastel Chocolate'", resultado.Mensaje);
        }
    }
}
