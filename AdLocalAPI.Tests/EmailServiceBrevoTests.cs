using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.Constants;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class EmailServiceBrevoTests
    {
        [Fact]
        public async Task EnviarCorreoAsync_DestinatarioVacio_LanzaArgumentException()
        {
            var mockRepo = new Mock<IConfiguracionRepository>();
            var mockConfig = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<EmailService>>();

            var service = new EmailService(mockRepo.Object, mockConfig.Object, mockLogger.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.EnviarCorreoAsync("", "Asunto", "<p>Hola</p>")
            );
        }

        [Fact]
        public async Task EnviarCorreoAsync_SinConfiguracion_LanzaInvalidOperationException()
        {
            var mockRepo = new Mock<IConfiguracionRepository>();
            mockRepo.Setup(r => r.ObtenerTodosAsync())
                .ReturnsAsync(new List<ConfiguracionSistema>());

            var mockConfig = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<EmailService>>();

            var service = new EmailService(mockRepo.Object, mockConfig.Object, mockLogger.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.EnviarCorreoAsync("destino@adlocal.mx", "Asunto", "<p>Hola</p>")
            );

            Assert.Contains("No se han configurado credenciales válidas", ex.Message);
        }

        [Fact]
        public async Task EnviarCorreoAsync_ConBrevoApiKey_EnviaViaHttpApiExitosamente()
        {
            var mockRepo = new Mock<IConfiguracionRepository>();
            mockRepo.Setup(r => r.ObtenerTodosAsync())
                .ReturnsAsync(new List<ConfiguracionSistema>
                {
                    new ConfiguracionSistema { Key = "BREVO_API_KEY", Val = "xkeysib-test-fake-key-12345" },
                    new ConfiguracionSistema { Key = ConfiguracionKeys.EmailFrom, Val = "notificaciones@adlocal.mx" },
                    new ConfiguracionSistema { Key = ConfiguracionKeys.EmailFromNombre, Val = "AdLocal Notificaciones" }
                });

            var mockConfig = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<EmailService>>();

            // Mock de HttpMessageHandler para simular respuesta 201 Created de Brevo
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri != null &&
                        req.RequestUri.ToString().Contains("api.brevo.com/v3/smtp/email") &&
                        req.Headers.Contains("api-key")
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent("{\"messageId\":\"<test-123@smtp-relay.mailin.fr>\"}")
                });

            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(mockHttpMessageHandler.Object));

            var service = new EmailService(mockRepo.Object, mockConfig.Object, mockLogger.Object, mockFactory.Object);

            // No debe lanzar excepción ya que el HTTP API de Brevo respondió 201 Created
            await service.EnviarCorreoAsync("cliente@gmail.com", "Código de Verificación", "<p>123456</p>");

            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("api.brevo.com")
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task EnviarCorreoAsync_ConKeyPrefijoXkeysib_DetectaBrevoApiAutomaticamente()
        {
            var mockRepo = new Mock<IConfiguracionRepository>();
            mockRepo.Setup(r => r.ObtenerTodosAsync())
                .ReturnsAsync(new List<ConfiguracionSistema>
                {
                    // Usuario puso la API key en EMAIL_KEY en lugar de clave SMTP
                    new ConfiguracionSistema { Key = ConfiguracionKeys.EmailKey, Val = "xkeysib-987654321-auto-detect" },
                    new ConfiguracionSistema { Key = ConfiguracionKeys.EmailFrom, Val = "contacto@adlocal.mx" },
                    new ConfiguracionSistema { Key = ConfiguracionKeys.EmailUser, Val = "cuenta@adlocal.mx" }
                });

            var mockConfig = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<EmailService>>();

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent("{\"messageId\":\"<auto-detect-id>\"}")
                });

            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(mockHttpMessageHandler.Object));

            var service = new EmailService(mockRepo.Object, mockConfig.Object, mockLogger.Object, mockFactory.Object);

            await service.EnviarCorreoAsync("usuario@gmail.com", "Recuperar contraseña", "<p>Enlace</p>");

            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
