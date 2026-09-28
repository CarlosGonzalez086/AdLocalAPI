using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class Fase3DomainServicesTests
    {
        private JwtContext CreateJwtContext(long userId = 10, string role = "Comercio")
        {
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim("id", userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            return new JwtContext(accessor);
        }

        // ==========================================
        // NOTIFICACION SERVICE TESTS
        // ==========================================
        [Fact]
        public async Task NotificacionService_ObtenerAsync_RetornaResumenCorrecto()
        {
            // Arrange
            var mockRepo = new Mock<INotificacionRepository>();
            var jwt = CreateJwtContext(userId: 55, role: RolesUsuario.Cliente);

            var items = new List<NotificacionDto>
            {
                new NotificacionDto { Uuid = Guid.NewGuid(), Titulo = "Pedido listo", Mensaje = "Tu pedido está listo", Leida = false }
            };

            mockRepo.Setup(r => r.ObtenerPorUsuarioAsync(55, RolesUsuario.Cliente, 20))
                .ReturnsAsync(items);
            mockRepo.Setup(r => r.ContarNoLeidasAsync(55))
                .ReturnsAsync(1);

            var service = new NotificacionService(mockRepo.Object, jwt);

            // Act
            var response = await service.ObtenerAsync(20);

            // Assert
            Assert.Equal("200", response.Codigo);
            Assert.NotNull(response.Respuesta);
            Assert.Equal(1, response.Respuesta.NoLeidas);
            Assert.Single(response.Respuesta.Notificaciones);
            mockRepo.Verify(r => r.ObtenerPorUsuarioAsync(55, RolesUsuario.Cliente, 20), Times.Once);
            mockRepo.Verify(r => r.ContarNoLeidasAsync(55), Times.Once);
        }

        [Fact]
        public async Task NotificacionService_MarcarLeidaAsync_NoExiste_Retorna404()
        {
            // Arrange
            var mockRepo = new Mock<INotificacionRepository>();
            var jwt = CreateJwtContext(userId: 12);
            var notifId = Guid.NewGuid();

            mockRepo.Setup(r => r.MarcarLeidaAsync(notifId, 12))
                .ReturnsAsync(false);

            var service = new NotificacionService(mockRepo.Object, jwt);

            // Act
            var response = await service.MarcarLeidaAsync(notifId);

            // Assert
            Assert.Equal("404", response.Codigo);
            Assert.Equal("Notificación no encontrada.", response.Mensaje);
        }

        [Fact]
        public async Task NotificacionService_MarcarLeidaAsync_Existe_RetornaExito()
        {
            // Arrange
            var mockRepo = new Mock<INotificacionRepository>();
            var jwt = CreateJwtContext(userId: 12);
            var notifId = Guid.NewGuid();

            mockRepo.Setup(r => r.MarcarLeidaAsync(notifId, 12))
                .ReturnsAsync(true);

            var service = new NotificacionService(mockRepo.Object, jwt);

            // Act
            var response = await service.MarcarLeidaAsync(notifId);

            // Assert
            Assert.Equal("200", response.Codigo);
            mockRepo.Verify(r => r.MarcarLeidaAsync(notifId, 12), Times.Once);
        }

        [Fact]
        public async Task NotificacionService_MarcarTodasLeidasAsync_InvocaRepositorio()
        {
            // Arrange
            var mockRepo = new Mock<INotificacionRepository>();
            var jwt = CreateJwtContext(userId: 33);

            mockRepo.Setup(r => r.MarcarTodasLeidasAsync(33))
                .ReturnsAsync(4);

            var service = new NotificacionService(mockRepo.Object, jwt);

            // Act
            var response = await service.MarcarTodasLeidasAsync();

            // Assert
            Assert.Equal("200", response.Codigo);
            mockRepo.Verify(r => r.MarcarTodasLeidasAsync(33), Times.Once);
        }

        [Fact]
        public async Task NotificacionService_NotificarComercioAsync_CreaNotificacionesParaDestinatarios()
        {
            // Arrange
            var mockRepo = new Mock<INotificacionRepository>();
            var jwt = CreateJwtContext();

            var pedido = new Pedido { Id = 100, IdComercio = 20, IdUsuario = 5, NumeroPedido = "ORD-1" };
            mockRepo.Setup(r => r.ObtenerDestinatariosComercioAsync(20))
                .ReturnsAsync(new List<long> { 7, 8 });

            var service = new NotificacionService(mockRepo.Object, jwt);

            // Act
            await service.NotificarComercioAsync(pedido, TipoNotificacionPedido.PedidoCreado, "Nuevo Pedido", "Tienes una orden");

            // Assert
            mockRepo.Verify(r => r.CrearNotificacionesAsync(It.Is<IEnumerable<Notificacion>>(n => n != null)), Times.Once);
        }

        // ==========================================
        // COMISION SERVICE TESTS
        // ==========================================
        [Fact]
        public async Task ComisionService_RegistrarVentaAsync_NoPagado_NoRegistra()
        {
            // Arrange
            var mockRepo = new Mock<IComisionRepository>();
            var service = new ComisionService(mockRepo.Object);

            var pedido = new Pedido
            {
                Id = 1,
                EstadoPago = EstadoPagoPedido.Pendiente,
                MontoComision = 15m
            };

            // Act
            await service.RegistrarVentaAsync(pedido);

            // Assert
            mockRepo.Verify(r => r.AgregarAsync(It.IsAny<Comision>()), Times.Never);
        }

        [Fact]
        public async Task ComisionService_RegistrarVentaAsync_YaExiste_NoDuplica()
        {
            // Arrange
            var mockRepo = new Mock<IComisionRepository>();
            var service = new ComisionService(mockRepo.Object);

            var pedido = new Pedido
            {
                Id = 1,
                EstadoPago = EstadoPagoPedido.Pagado,
                MontoComision = 15m
            };

            mockRepo.Setup(r => r.ExisteOperacionAsync((int)TipoOperacionComision.Venta, 1))
                .ReturnsAsync(true);

            // Act
            await service.RegistrarVentaAsync(pedido);

            // Assert
            mockRepo.Verify(r => r.AgregarAsync(It.IsAny<Comision>()), Times.Never);
        }

        [Fact]
        public async Task ComisionService_RegistrarVentaAsync_Nuevo_AgregaComision()
        {
            // Arrange
            var mockRepo = new Mock<IComisionRepository>();
            var service = new ComisionService(mockRepo.Object);

            var pedido = new Pedido
            {
                Id = 2,
                IdComercio = 10,
                Total = 100m,
                PorcentajeComision = 10m,
                MontoComision = 10m,
                EstadoPago = EstadoPagoPedido.Pagado,
                NumeroPedido = "PED-002",
                ComisionFija = 0
            };

            mockRepo.Setup(r => r.ExisteOperacionAsync((int)TipoOperacionComision.Venta, 2))
                .ReturnsAsync(false);

            // Act
            await service.RegistrarVentaAsync(pedido);

            // Assert
            mockRepo.Verify(r => r.AgregarAsync(It.Is<Comision>(c =>
                c.IdComercio == 10 &&
                c.MontoOperacion == 100m &&
                c.MontoComision == 10m &&
                c.Estatus == (int)EstatusComision.Pendiente
            )), Times.Once);
        }

        [Fact]
        public async Task ComisionService_LiquidarAsync_SinPendientes_Retorna404()
        {
            // Arrange
            var mockRepo = new Mock<IComisionRepository>();
            var service = new ComisionService(mockRepo.Object);

            mockRepo.Setup(r => r.ObtenerPendientesParaLiquidacionAsync(5, It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Comision>());

            // Act
            var response = await service.LiquidarAsync(5, "mes");

            // Assert
            Assert.Equal("404", response.Codigo);
            mockRepo.Verify(r => r.ActualizarComisionesAsync(It.IsAny<List<Comision>>()), Times.Never);
        }

        [Fact]
        public async Task ComisionService_LiquidarAsync_ConPendientes_ActualizaAPagadas()
        {
            // Arrange
            var mockRepo = new Mock<IComisionRepository>();
            var service = new ComisionService(mockRepo.Object);

            var lista = new List<Comision>
            {
                new Comision { Id = 1, Estatus = (int)EstatusComision.Pendiente, MontoComision = 50m },
                new Comision { Id = 2, Estatus = (int)EstatusComision.Pendiente, MontoComision = 25m }
            };

            mockRepo.Setup(r => r.ObtenerPendientesParaLiquidacionAsync(5, It.IsAny<DateTime>()))
                .ReturnsAsync(lista);

            // Act
            var response = await service.LiquidarAsync(5, "semana");

            // Assert
            Assert.Equal("200", response.Codigo);
            Assert.All(lista, c => Assert.Equal((int)EstatusComision.Pagada, c.Estatus));
            Assert.All(lista, c => Assert.NotNull(c.FechaPago));
            mockRepo.Verify(r => r.ActualizarComisionesAsync(lista), Times.Once);
        }

        // ==========================================
        // SUSCRIPCION SERVICE TESTS
        // ==========================================
        [Fact]
        public async Task SuscripcionService_SuscribirseConTarjeta_PlanInactivo_Retorna404()
        {
            // Arrange
            var mockPlanRepo = new Mock<IPlanRepository>();
            var mockSubRepo = new Mock<ISuscripcionRepository>();
            var mockUserRepo = new Mock<IUsuarioRepository>();
            var mockEnv = new Mock<IWebHostEnvironment>();
            var jwt = CreateJwtContext();

            mockPlanRepo.Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync(new Plan { Id = 99, Activo = false });

            var service = new SuscripcionService(jwt, mockUserRepo.Object, mockSubRepo.Object, mockPlanRepo.Object, mockEnv.Object);

            // Act
            var response = await service.SuscribirseConTarjeta(99, "pm_test", false);

            // Assert
            Assert.Equal("404", response.Codigo);
            Assert.Equal("Plan no encontrado", response.Mensaje);
        }

        [Fact]
        public async Task SuscripcionService_CancelarPlan_SinSuscripcionActiva_Retorna404()
        {
            // Arrange
            var mockPlanRepo = new Mock<IPlanRepository>();
            var mockSubRepo = new Mock<ISuscripcionRepository>();
            var mockUserRepo = new Mock<IUsuarioRepository>();
            var mockEnv = new Mock<IWebHostEnvironment>();
            var jwt = CreateJwtContext(userId: 15);

            mockSubRepo.Setup(r => r.GetActivaByUsuarioAsync(15))
                .ReturnsAsync((Suscripcion?)null);

            var service = new SuscripcionService(jwt, mockUserRepo.Object, mockSubRepo.Object, mockPlanRepo.Object, mockEnv.Object);

            // Act
            var response = await service.CancelarPlan();

            // Assert
            Assert.Equal("404", response.Codigo);
            Assert.Equal("No hay suscripción activa", response.Mensaje);
        }
    }
}
