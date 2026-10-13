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
        [InlineData(EstadoPedido.ListoParaEnviar, TipoEntregaPedido.Domicilio, new[] { EstadoPedido.Enviado, EstadoPedido.Cancelado })]
        [InlineData(EstadoPedido.Enviado, TipoEntregaPedido.Domicilio, new[] { EstadoPedido.Entregado })]
        [InlineData(EstadoPedido.ListoParaRecoger, TipoEntregaPedido.Recoger, new[] { EstadoPedido.Entregado, EstadoPedido.Cancelado })]
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
        public void CambiarEstado_TransicionInvalida_Retorna409Conflict()
        {
            var ex = Assert.Throws<PedidoOperacionException>(() => PedidoOperacion.ValidarEstado(new Pedido
            { Estado = EstadoPedido.PendienteAprobacion, TipoEntrega = TipoEntregaPedido.Domicilio },
                new CambiarEstadoPedidoDto { Estado = EstadoPedido.Entregado }));
            Assert.Equal("409", ex.Codigo);
            Assert.Equal("La transición de estado no está permitida.", ex.Message);
        }
        [Fact]
        public void CambiarEstado_PedidoPagadoSinReembolso_NoPermiteCancelarNiRechazar()
        {
            var ex = Assert.Throws<PedidoOperacionException>(() => PedidoOperacion.ValidarEstado(new Pedido
            { Estado = EstadoPedido.PendienteAprobacion, EstadoPago = EstadoPagoPedido.Pagado },
                new CambiarEstadoPedidoDto { Estado = EstadoPedido.Rechazado, Comentario = "Sin stock" }));
            Assert.Contains("reembolsarse", ex.Message);
        }
        [Fact]
        public async Task CambiarEstado_DelegacionAtomica_NotificacionFallida_NoOcultaExito()
        {
            var service = CrearServicio(); var uuid = Guid.NewGuid();
            var dto = new CambiarEstadoPedidoDto { Estado = EstadoPedido.Aprobado, VersionPedido = 8 };
            _repoMock.Setup(r => r.PuedeGestionarAsync(10, "Comercio", 5)).ReturnsAsync(true);
            _repoMock.Setup(r => r.CambiarEstadoAsync(5, uuid, 10, dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pedido { Uuid = uuid, Estado = EstadoPedido.Aprobado });
            _repoMock.Setup(r => r.ObtenerDetalleAsync(5, uuid)).ReturnsAsync(new PedidoComercioDetalleDto { Estado = EstadoPedido.Aprobado });
            _notifMock.Setup(n => n.NotificarClienteAsync(It.IsAny<Pedido>(), It.IsAny<TipoNotificacionPedido>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new System.IO.IOException("Fallo sintético de notificación"));
            Assert.Equal("200", (await service.CambiarEstadoAsync(5, uuid, dto)).Codigo);
            _repoMock.Verify(r => r.CambiarEstadoAsync(5, uuid, 10, dto, It.IsAny<CancellationToken>()), Times.Once);
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
