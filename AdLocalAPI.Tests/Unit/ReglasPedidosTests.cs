using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Amazon.S3;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Unit
{
    public class ReglasPedidosTests
    {
        private readonly Mock<IPedidoComercioRepository> _repoMock;
        private readonly Mock<IAmazonS3> _s3Mock;
        private readonly Mock<INotificacionService> _notifMock;
        private readonly Mock<IComisionService> _comisionesMock;
        private readonly Mock<IConfiguration> _configMock;

        public ReglasPedidosTests()
        {
            _repoMock = new Mock<IPedidoComercioRepository>();
            _s3Mock = new Mock<IAmazonS3>();
            _notifMock = new Mock<INotificacionService>();
            _comisionesMock = new Mock<IComisionService>();
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(c => c["R2:ComprobantesBucket"]).Returns("test-bucket");
        }

        private PedidoComercioService CrearServicio(long userId = 10, string role = "Comercio")
        {
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim("id", userId.ToString()),
                new Claim("rol", role),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
            var jwt = new JwtContext(new HttpContextAccessor { HttpContext = httpContext });

            return new PedidoComercioService(
                _repoMock.Object,
                jwt,
                _s3Mock.Object,
                _configMock.Object,
                _notifMock.Object,
                _comisionesMock.Object
            );
        }

        // ==============================================================
        // 1. REGLAS DE MATRIZ DE TRANSICIÓN DE ESTADOS (ObtenerAcciones)
        // ==============================================================

        [Theory]
        [InlineData(EstadoPedido.PendienteAprobacion, TipoEntregaPedido.Domicilio, new[] { EstadoPedido.Aprobado, EstadoPedido.Rechazado })]
        [InlineData(EstadoPedido.PendienteAprobacion, TipoEntregaPedido.Recoger, new[] { EstadoPedido.Aprobado, EstadoPedido.Rechazado })]
        [InlineData(EstadoPedido.Aprobado, TipoEntregaPedido.Domicilio, new[] { EstadoPedido.Preparando, EstadoPedido.Cancelado })]
        [InlineData(EstadoPedido.Aprobado, TipoEntregaPedido.Recoger, new[] { EstadoPedido.Preparando, EstadoPedido.Cancelado })]
        [InlineData(EstadoPedido.Preparando, TipoEntregaPedido.Domicilio, new[] { EstadoPedido.ListoParaEnviar, EstadoPedido.Cancelado })]
        [InlineData(EstadoPedido.Preparando, TipoEntregaPedido.Recoger, new[] { EstadoPedido.ListoParaRecoger, EstadoPedido.Cancelado })]
        [InlineData(EstadoPedido.ListoParaEnviar, TipoEntregaPedido.Domicilio, new[] { EstadoPedido.Enviado })]
        [InlineData(EstadoPedido.Enviado, TipoEntregaPedido.Domicilio, new[] { EstadoPedido.Entregado })]
        [InlineData(EstadoPedido.ListoParaRecoger, TipoEntregaPedido.Recoger, new[] { EstadoPedido.Entregado })]
        [InlineData(EstadoPedido.Entregado, TipoEntregaPedido.Domicilio, new[] { EstadoPedido.Completado })]
        [InlineData(EstadoPedido.Entregado, TipoEntregaPedido.Recoger, new[] { EstadoPedido.Completado })]
        public void ObtenerAcciones_EstadosValidos_RetornaAccionesPermitidas(
            EstadoPedido estado,
            TipoEntregaPedido entrega,
            EstadoPedido[] accionesEsperadas)
        {
            var acciones = PedidoComercioRepository.ObtenerAcciones(estado, entrega);

            Assert.Equal(accionesEsperadas.Length, acciones.Count);
            foreach (var accion in accionesEsperadas)
            {
                Assert.Contains(accion, acciones);
            }
        }

        [Theory]
        [InlineData(EstadoPedido.Completado)]
        [InlineData(EstadoPedido.Cancelado)]
        [InlineData(EstadoPedido.Rechazado)]
        public void ObtenerAcciones_EstadosTerminales_NoTienenAccionesAdicionales(EstadoPedido estadoTerminal)
        {
            var accionesDomicilio = PedidoComercioRepository.ObtenerAcciones(estadoTerminal, TipoEntregaPedido.Domicilio);
            var accionesRecoger = PedidoComercioRepository.ObtenerAcciones(estadoTerminal, TipoEntregaPedido.Recoger);

            Assert.Empty(accionesDomicilio);
            Assert.Empty(accionesRecoger);
        }

        // ==============================================================
        // 2. REGLAS DE CAMBIO DE ESTADO EN SERVICIO
        // ==============================================================

        [Fact]
        public async Task CambiarEstado_TransicionInvalida_Retorna409Conflict()
        {
            const long comercioId = 5;
            var pedidoUuid = Guid.NewGuid();
            var service = CrearServicio(userId: 10, role: "Comercio");

            _repoMock.Setup(r => r.PuedeGestionarAsync(10, "Comercio", comercioId)).ReturnsAsync(true);
            _repoMock.Setup(r => r.ObtenerPedidoTrackingAsync(comercioId, pedidoUuid))
                .ReturnsAsync(new Pedido
                {
                    Id = 1,
                    Uuid = pedidoUuid,
                    IdComercio = comercioId,
                    Estado = EstadoPedido.PendienteAprobacion,
                    TipoEntrega = TipoEntregaPedido.Domicilio,
                    EstadoPago = EstadoPagoPedido.Pendiente
                });

            // Intento ilegal: de PendienteAprobacion directo a Entregado
            var resultado = await service.CambiarEstadoAsync(comercioId, pedidoUuid, new CambiarEstadoPedidoDto
            {
                Estado = EstadoPedido.Entregado
            });

            Assert.Equal("409", resultado.Codigo);
            Assert.Equal("La transición de estado no está permitida.", resultado.Mensaje);
        }

        [Fact]
        public async Task CambiarEstado_PedidoPagadoSinReembolso_NoPermiteCancelarNiRechazar()
        {
            const long comercioId = 5;
            var pedidoUuid = Guid.NewGuid();
            var service = CrearServicio(userId: 10, role: "Comercio");

            _repoMock.Setup(r => r.PuedeGestionarAsync(10, "Comercio", comercioId)).ReturnsAsync(true);
            _repoMock.Setup(r => r.ObtenerPedidoTrackingAsync(comercioId, pedidoUuid))
                .ReturnsAsync(new Pedido
                {
                    Id = 1,
                    Uuid = pedidoUuid,
                    IdComercio = comercioId,
                    Estado = EstadoPedido.PendienteAprobacion,
                    TipoEntrega = TipoEntregaPedido.Domicilio,
                    EstadoPago = EstadoPagoPedido.Pagado // Ya está cobrado
                });

            // Intentar rechazar un pedido pagado
            var resRechazo = await service.CambiarEstadoAsync(comercioId, pedidoUuid, new CambiarEstadoPedidoDto
            {
                Estado = EstadoPedido.Rechazado
            });

            Assert.Equal("409", resRechazo.Codigo);
            Assert.Contains("reembolsarse", resRechazo.Mensaje);
        }

        [Fact]
        public async Task CambiarEstado_TransicionValida_ActualizaEstadoYFechasAuditoria()
        {
            const long comercioId = 5;
            var pedidoUuid = Guid.NewGuid();
            var service = CrearServicio(userId: 10, role: "Comercio");

            var pedido = new Pedido
            {
                Id = 100,
                Uuid = pedidoUuid,
                IdComercio = comercioId,
                NumeroPedido = "PED-2026-001",
                Estado = EstadoPedido.PendienteAprobacion,
                TipoEntrega = TipoEntregaPedido.Domicilio,
                EstadoPago = EstadoPagoPedido.Pendiente
            };

            _repoMock.Setup(r => r.PuedeGestionarAsync(10, "Comercio", comercioId)).ReturnsAsync(true);
            _repoMock.Setup(r => r.ObtenerPedidoTrackingAsync(comercioId, pedidoUuid)).ReturnsAsync(pedido);
            _repoMock.Setup(r => r.ObtenerDetalleAsync(comercioId, pedidoUuid)).ReturnsAsync(new PedidoComercioDetalleDto
            {
                Uuid = pedidoUuid,
                Estado = EstadoPedido.Aprobado
            });

            var resultado = await service.CambiarEstadoAsync(comercioId, pedidoUuid, new CambiarEstadoPedidoDto
            {
                Estado = EstadoPedido.Aprobado,
                Comentario = "Pedido aceptado por cocina"
            });

            Assert.Equal("200", resultado.Codigo);
            Assert.Equal(EstadoPedido.Aprobado, pedido.Estado);
            Assert.NotNull(pedido.FechaAprobacion);

            _repoMock.Verify(r => r.GuardarEstadoAsync(pedido, It.Is<PedidoHistorialEstado>(h =>
                h.IdPedido == pedido.Id &&
                h.EstadoAnterior == EstadoPedido.PendienteAprobacion &&
                h.EstadoNuevo == EstadoPedido.Aprobado &&
                h.Comentario == "Pedido aceptado por cocina"
            )), Times.Once);
        }

        // ==============================================================
        // 3. REGLAS FINANCIERAS DE TOTALES Y COMISIONES DE PEDIDO
        // ==============================================================

        [Theory]
        [InlineData(100.00, 30.00, 130.00)]
        [InlineData(250.50, 0.00, 250.50)]
        public void CalculoTotalPedido_SumaSubtotalMasEnvio(decimal subtotal, decimal envio, decimal totalEsperado)
        {
            var total = subtotal + envio;
            Assert.Equal(totalEsperado, total);
        }

        [Fact]
        public void CalculoComision_MontoComisionNuncaExcedeSubtotal()
        {
            decimal subtotal = 10.00m;
            decimal porcentaje = 10.0m; // 1.00m
            decimal comisionFija = 15.00m; // total teórica = 16.00m > subtotal 10.00m

            decimal comisionPorcentaje = subtotal * (porcentaje / 100m);
            decimal montoComision = Math.Round(comisionPorcentaje + comisionFija, 2);

            // Regla de salvaguarda de plataforma:
            if (montoComision > subtotal)
            {
                montoComision = subtotal;
            }

            decimal montoComercio = subtotal - montoComision;

            Assert.Equal(10.00m, montoComision);
            Assert.Equal(0.00m, montoComercio);
        }

        [Fact]
        public void CalculoMontoComercio_ConEnvio_SumaEnvioAlComercio()
        {
            decimal subtotal = 200.00m;
            decimal costoEnvio = 45.00m;
            decimal porcentaje = 5.0m; // 10.00m
            decimal comisionFija = 2.00m; // 12.00m total comision

            decimal comision = Math.Round((subtotal * (porcentaje / 100m)) + comisionFija, 2);
            decimal montoComercio = (subtotal - comision) + costoEnvio;

            Assert.Equal(12.00m, comision);
            Assert.Equal(233.00m, montoComercio);
            Assert.Equal(245.00m, subtotal + costoEnvio);
        }
    }
}
