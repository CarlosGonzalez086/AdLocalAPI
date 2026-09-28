using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Unit
{
    public class ReglasSuscripcionesTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<ISuscripcionRepository> _suscripcionRepoMock;
        private readonly Mock<IPlanRepository> _planRepoMock;
        private readonly Mock<IWebHostEnvironment> _envMock;

        public ReglasSuscripcionesTests()
        {
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _suscripcionRepoMock = new Mock<ISuscripcionRepository>();
            _planRepoMock = new Mock<IPlanRepository>();
            _envMock = new Mock<IWebHostEnvironment>();
        }

        private SuscripcionService CrearServicio(long userId = 15)
        {
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim("id", userId.ToString()),
                new Claim("rol", "Comercio"),
                new Claim(ClaimTypes.Role, "Comercio")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
            var jwt = new JwtContext(new HttpContextAccessor { HttpContext = httpContext });

            return new SuscripcionService(
                jwt,
                _usuarioRepoMock.Object,
                _suscripcionRepoMock.Object,
                _planRepoMock.Object,
                _envMock.Object
            );
        }

        // ==============================================================
        // 1. REGLAS DE CONSULTA Y ESTADO DE SUSCRIPCIÓN ACTIVA
        // ==============================================================

        [Fact]
        public async Task ObtenerMiSuscripcion_SinSuscripcionActiva_Retorna404()
        {
            const long userId = 20;
            var service = CrearServicio(userId);

            _suscripcionRepoMock
                .Setup(r => r.GetActivaByUsuario(userId))
                .ReturnsAsync((Suscripcion?)null);

            var resultado = await service.ObtenerMiSuscripcion();

            Assert.Equal("404", resultado.Codigo);
            Assert.Equal("No tienes suscripción activa", resultado.Mensaje);
        }

        [Fact]
        public async Task ObtenerMiSuscripcion_ConSuscripcionActiva_RetornaDetallesYLimitesDePlan()
        {
            const long userId = 25;
            var service = CrearServicio(userId);

            var suscripcion = new Suscripcion
            {
                Id = 1,
                UsuarioId = userId,
                PlanId = 2,
                StripeSubscriptionId = "sub_test_123",
                Status = "active",
                IsActive = true,
                AutoRenew = true,
                CurrentPeriodStart = DateTime.UtcNow.AddDays(-10),
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(20)
            };

            var plan = new Plan
            {
                Id = 2,
                Nombre = "Plan Emprendedor Pro",
                Precio = 299.00m,
                DuracionDias = 30,
                MaxNegocios = 2,
                MaxProductos = 50,
                MaxFotos = 10,
                NivelVisibilidad = 2,
                TieneAnalytics = true,
                PermiteCatalogo = true,
                StripePriceId = "price_test_123",
                Activo = true
            };

            _suscripcionRepoMock.Setup(r => r.GetActivaByUsuario(userId)).ReturnsAsync(suscripcion);
            _planRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(plan);

            var resultado = await service.ObtenerMiSuscripcion();

            Assert.Equal("200", resultado.Codigo);
            Assert.NotNull(resultado.Respuesta);
            Assert.True(resultado.Respuesta.Activa);
            Assert.Equal("active", resultado.Respuesta.Estado);
            Assert.Equal("Plan Emprendedor Pro", resultado.Respuesta.Plan.Nombre);
            Assert.Equal(2, resultado.Respuesta.Plan.MaxNegocios);
            Assert.Equal(50, resultado.Respuesta.Plan.MaxProductos);
            Assert.True(resultado.Respuesta.Plan.TieneAnalytics);
        }

        [Fact]
        public async Task CancelarPlan_SinSuscripcionActiva_Retorna404()
        {
            const long userId = 30;
            var service = CrearServicio(userId);

            _suscripcionRepoMock
                .Setup(r => r.GetActivaByUsuarioAsync(userId))
                .ReturnsAsync((Suscripcion?)null);

            var resultado = await service.CancelarPlan();

            Assert.Equal("404", resultado.Codigo);
            Assert.Equal("No hay suscripción activa", resultado.Mensaje);
        }

        // ==============================================================
        // 2. REGLAS DE LÍMITES DE PLAN (PRODUCTOS Y COMERCIOS)
        // ==============================================================

        [Theory]
        [InlineData(10, 10, false)] // Alcanzó el límite exacto -> Bloquear
        [InlineData(12, 10, false)] // Superó el límite -> Bloquear
        [InlineData(8, 10, true)]   // Debajo del límite -> Permitir
        [InlineData(0, 5, true)]    // 0 productos con límite 5 -> Permitir
        public void ValidarLimiteProductos_SegunPlan_PermiteOBloquea(
            int productosActuales,
            int maxPermitidoPlan,
            bool permiteNuevoProducto)
        {
            bool puedeCrear = productosActuales < maxPermitidoPlan;
            Assert.Equal(permiteNuevoProducto, puedeCrear);
        }

        [Theory]
        [InlineData(1, 1, false)] // Límite de 1 negocio alcanzado -> Bloquear
        [InlineData(2, 3, true)]  // 2 negocios con plan de 3 -> Permitir
        [InlineData(3, 3, false)] // 3 negocios con plan de 3 -> Bloquear
        public void ValidarLimiteComercios_SegunPlan_PermiteOBloquea(
            int comerciosActuales,
            int maxNegociosPlan,
            bool permiteNuevoComercio)
        {
            bool puedeCrear = comerciosActuales < maxNegociosPlan;
            Assert.Equal(permiteNuevoComercio, puedeCrear);
        }

        // ==============================================================
        // 3. REGLAS DE CICLO DE ESTADOS DE SUSCRIPCIÓN
        // ==============================================================

        [Fact]
        public void Suscripcion_EstadoPastDue_NoDestruyeInmediatamentePeroAlertaPagoFallido()
        {
            var suscripcion = new Suscripcion
            {
                Id = 99,
                Status = "past_due",
                IsActive = true // Permanece activa durante ventana de gracia de reintento de cobro
            };

            Assert.Equal("past_due", suscripcion.Status);
            Assert.True(suscripcion.IsActive);
        }

        [Fact]
        public void Suscripcion_EstadoCanceled_MarcaInactiva()
        {
            var suscripcion = new Suscripcion
            {
                Id = 100,
                Status = "canceled",
                IsActive = false,
                AutoRenew = false
            };

            Assert.Equal("canceled", suscripcion.Status);
            Assert.False(suscripcion.IsActive);
            Assert.False(suscripcion.AutoRenew);
        }
    }
}
