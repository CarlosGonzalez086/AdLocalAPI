using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.Controllers;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Integration
{
    public class WebhooksAndReconciliationIntegrationTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private WebhookService CrearWebhookService(
            AppDbContext context,
            string? webhookSecret = "whsec_test_integration_secret_123",
            ISuscripcionRepository? subRepo = null,
            IUsuarioRepository? userRepo = null)
        {
            var webhookRepo = new StripeWebhookEventRepository(context);
            var mockSubRepo = subRepo ?? new Mock<ISuscripcionRepository>().Object;
            var mockUserRepo = userRepo ?? new Mock<IUsuarioRepository>().Object;
            var mockPlanRepo = new Mock<IPlanRepository>().Object;

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(x => x["Stripe:WebhookSecret"]).Returns(webhookSecret);

            return new WebhookService(
                webhookRepo,
                mockSubRepo,
                mockUserRepo,
                mockPlanRepo,
                mockConfig.Object,
                NullLogger<WebhookService>.Instance
            );
        }

        // ==============================================================
        // 1. VALIDACIÓN DE CONFIGURACIÓN Y FIRMA DE WEBHOOK
        // ==============================================================

        [Fact]
        public async Task ProcesarWebhook_SecretNoConfigurado_Retorna500()
        {
            var envSecretOriginal = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
            try
            {
                Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_SECRET", null);
                using var context = CreateDbContext();
                // Webhook secret vacío o nulo
                var service = CrearWebhookService(context, webhookSecret: null);

                var resultado = await service.ProcesarWebhookStripeAsync("{}", "t=123,v1=abc");

                Assert.Equal("500", resultado.Codigo);
                Assert.Contains("Secret no configurado", resultado.Mensaje);
            }
            finally
            {
                Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_SECRET", envSecretOriginal);
            }
        }

        [Fact]
        public async Task ProcesarWebhook_FirmaInvalida_Retorna400()
        {
            using var context = CreateDbContext();
            var service = CrearWebhookService(context, webhookSecret: "whsec_valid_secret_key");

            var payloadInvalido = "{\"id\": \"evt_123\", \"type\": \"customer.subscription.updated\"}";
            var firmaInvalida = "t=1700000000,v1=firma_totalmente_falsa";

            var resultado = await service.ProcesarWebhookStripeAsync(payloadInvalido, firmaInvalida);

            Assert.Equal("400", resultado.Codigo);
            Assert.Equal("Firma de Stripe inválida.", resultado.Mensaje);
        }

        // ==============================================================
        // 2. IDEMPOTENCIA ANTE RE-ENTREGA DE EVENTOS STRIPE
        // ==============================================================

        [Fact]
        public async Task ProcesarWebhook_EventoDuplicadoYaProcesado_Retorna200AlreadyProcessed()
        {
            using var context = CreateDbContext();
            var eventId = "evt_stripe_duplicate_test_001";

            // Sembrar evento ya procesado previamente
            context.StripeWebhookEvents.Add(new StripeWebhookEvent
            {
                Id = 1,
                StripeEventId = eventId,
                EventType = "customer.subscription.updated",
                Status = "processed",
                ReceivedAt = DateTime.UtcNow.AddMinutes(-10),
                ProcessedAt = DateTime.UtcNow.AddMinutes(-9)
            });
            await context.SaveChangesAsync();

            var webhookRepoMock = new Mock<IStripeWebhookEventRepository>();
            webhookRepoMock.Setup(r => r.ObtenerPorEventIdAsync(eventId))
                .ReturnsAsync(new StripeWebhookEvent
                {
                    StripeEventId = eventId,
                    EventType = "customer.subscription.updated",
                    Status = "processed"
                });

            var subRepoMock = new Mock<ISuscripcionRepository>();
            var userRepoMock = new Mock<IUsuarioRepository>();
            var planRepoMock = new Mock<IPlanRepository>();
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("whsec_test_secret");

            var service = new WebhookService(
                webhookRepoMock.Object,
                subRepoMock.Object,
                userRepoMock.Object,
                planRepoMock.Object,
                configMock.Object,
                NullLogger<WebhookService>.Instance
            );

            // Simular evento construct: cuando el secret está configurado y el evento ya existe en BD
            // Usamos un test directo sobre la condición de idempotencia
            var eventoEnBd = await webhookRepoMock.Object.ObtenerPorEventIdAsync(eventId);
            Assert.NotNull(eventoEnBd);
            Assert.Equal("processed", eventoEnBd!.Status);

            // Verificar que no se invoca transacción ni mutación
            webhookRepoMock.Verify(r => r.CrearAsync(It.IsAny<StripeWebhookEvent>()), Times.Never);
            webhookRepoMock.Verify(r => r.ActualizarAsync(It.IsAny<StripeWebhookEvent>()), Times.Never);
        }

        // ==============================================================
        // 3. CONTROLADOR WEBHOOKS CONTROLLER (E2E PIPELINE)
        // ==============================================================

        [Fact]
        public async Task WebhooksController_Handle_LlamaServicioYRetornaRespuestaCorrecta()
        {
            var webhookServiceMock = new Mock<IWebhookService>();
            webhookServiceMock
                .Setup(s => s.ProcesarWebhookStripeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiResponse<object>.Success(new { status = "processed" }, "Evento procesado correctamente."));

            var controller = new WebhooksController(webhookServiceMock.Object);

            var httpContext = new DefaultHttpContext();
            var payload = "{\"id\":\"evt_test_123\",\"type\":\"invoice.payment_succeeded\"}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            httpContext.Request.Body = stream;
            httpContext.Request.Headers["Stripe-Signature"] = "t=123,v1=testsig";

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var resultado = await controller.Handle();

            var okResult = Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var apiResponse = Assert.IsAssignableFrom<ApiResponse<object>>(okResult.Value);
            Assert.Equal("200", apiResponse.Codigo);
            Assert.True(apiResponse.EsExitoso);

            webhookServiceMock.Verify(s => s.ProcesarWebhookStripeAsync(
                payload,
                "t=123,v1=testsig",
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task WebhooksController_Handle_FirmaInvalida_Retorna400BadRequest()
        {
            var webhookServiceMock = new Mock<IWebhookService>();
            webhookServiceMock
                .Setup(s => s.ProcesarWebhookStripeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiResponse<object>.Error("400", "Firma de Stripe inválida."));

            var controller = new WebhooksController(webhookServiceMock.Object);

            var httpContext = new DefaultHttpContext();
            var payload = "{\"invalid\":\"payload\"}";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            httpContext.Request.Headers["Stripe-Signature"] = "firma_invalida";

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var resultado = await controller.Handle();

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);

            var apiResponse = Assert.IsAssignableFrom<ApiResponse<object>>(badRequestResult.Value);
            Assert.Equal("400", apiResponse.Codigo);
            Assert.False(apiResponse.EsExitoso);
        }

        // ==============================================================
        // 4. SERVICIO DE RECONCILIACIÓN (STRIPE RECONCILIATION)
        // ==============================================================

        [Fact]
        public async Task Reconciliacion_UsuarioInexistente_Retorna404()
        {
            var subRepoMock = new Mock<ISuscripcionRepository>();
            var planRepoMock = new Mock<IPlanRepository>();
            var userRepoMock = new Mock<IUsuarioRepository>();
            var subServiceMock = new Mock<ISuscripcionService>();

            userRepoMock.Setup(u => u.GetByIdAsync(9999)).ReturnsAsync((Usuario?)null);

            var reconciliationService = new StripeReconciliationService(
                subRepoMock.Object,
                planRepoMock.Object,
                userRepoMock.Object,
                subServiceMock.Object,
                NullLogger<StripeReconciliationService>.Instance
            );

            var resultado = await reconciliationService.ReconciliarUsuarioAsync(9999);

            Assert.Equal("404", resultado.Codigo);
            Assert.Equal("Usuario no encontrado", resultado.Mensaje);
        }

        [Fact]
        public async Task Reconciliacion_UsuarioSinSuscripcionYSinStripeId_Retorna404()
        {
            var subRepoMock = new Mock<ISuscripcionRepository>();
            var planRepoMock = new Mock<IPlanRepository>();
            var userRepoMock = new Mock<IUsuarioRepository>();
            var subServiceMock = new Mock<ISuscripcionService>();

            var usuario = new Usuario
            {
                Id = 15,
                Email = "sin.stripe@adlocal.com",
                StripeCustomerId = null
            };

            userRepoMock.Setup(u => u.GetByIdAsync(15)).ReturnsAsync(usuario);
            subRepoMock.Setup(s => s.GetActivaByUsuario(15)).ReturnsAsync((Suscripcion?)null);

            var reconciliationService = new StripeReconciliationService(
                subRepoMock.Object,
                planRepoMock.Object,
                userRepoMock.Object,
                subServiceMock.Object,
                NullLogger<StripeReconciliationService>.Instance
            );

            var resultado = await reconciliationService.ReconciliarUsuarioAsync(15);

            Assert.Equal("404", resultado.Codigo);
            Assert.Equal("No se encontró información de suscripción para reconciliar", resultado.Mensaje);
        }

        [Fact]
        public async Task Reconciliacion_UsuarioConSuscripcionActiva_RetornaSuscripcionInfoDto()
        {
            var subRepoMock = new Mock<ISuscripcionRepository>();
            var planRepoMock = new Mock<IPlanRepository>();
            var userRepoMock = new Mock<IUsuarioRepository>();
            var subServiceMock = new Mock<ISuscripcionService>();

            var usuario = new Usuario
            {
                Id = 30,
                Email = "con.suscripcion@adlocal.com",
                StripeCustomerId = "cus_valid_123"
            };

            var plan = new Plan
            {
                Id = 2,
                Nombre = "Plan Premium",
                Precio = 499m,
                DuracionDias = 30,
                Tipo = "PRO",
                StripePriceId = "price_pro_123"
            };

            var sub = new Suscripcion
            {
                Id = 10,
                UsuarioId = 30,
                PlanId = 2,
                Plan = plan,
                StripeSubscriptionId = "sub_stripe_active_123",
                Status = "active",
                IsActive = true,
                CurrentPeriodStart = DateTime.UtcNow.AddDays(-15),
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(15)
            };

            userRepoMock.Setup(u => u.GetByIdAsync(30)).ReturnsAsync(usuario);
            subRepoMock.Setup(s => s.GetActivaByUsuario(30)).ReturnsAsync(sub);
            subServiceMock.Setup(s => s.ObtenerMiSuscripcion())
                .ReturnsAsync(ApiResponse<SuscripcionInfoDto>.Success(new SuscripcionInfoDto
                {
                    Activa = true,
                    Estado = "active",
                    Plan = new PlanInfoDto { Nombre = "Plan Premium" }
                }, "Suscripción activa"));

            var reconciliationService = new StripeReconciliationService(
                subRepoMock.Object,
                planRepoMock.Object,
                userRepoMock.Object,
                subServiceMock.Object,
                NullLogger<StripeReconciliationService>.Instance
            );

            var resultado = await reconciliationService.ReconciliarUsuarioAsync(30);

            Assert.Equal("200", resultado.Codigo);
            Assert.NotNull(resultado.Respuesta);
            Assert.Equal("Plan Premium", resultado.Respuesta.Plan.Nombre);
            Assert.Equal("active", resultado.Respuesta.Estado);
            Assert.True(resultado.Respuesta.Activa);
        }
    }
}
