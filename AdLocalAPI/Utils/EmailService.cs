using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AdLocalAPI.Constants;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AdLocalAPI.Utils
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguracionRepository _repository;
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpClientFactory? _httpClientFactory;

        public EmailService(
            IConfiguracionRepository repository,
            IConfiguration config,
            ILogger<EmailService> logger,
            IHttpClientFactory? httpClientFactory = null)
        {
            _repository = repository;
            _config = config;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task EnviarCorreoAsync(
            string para,
            string asunto,
            string htmlContenido)
        {
            if (string.IsNullOrWhiteSpace(para))
            {
                throw new ArgumentException("El correo destinatario es requerido.", nameof(para));
            }

            // ==========================================
            // 1. OBTENER CONFIGURACIÓN DESDE BASE DE DATOS
            // ==========================================
            var configuraciones = await _repository.ObtenerTodosAsync();

            var configDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (configuraciones != null)
            {
                foreach (var c in configuraciones)
                {
                    if (!string.IsNullOrWhiteSpace(c.Key))
                    {
                        configDict[c.Key.Trim()] = c.Val?.Trim() ?? string.Empty;
                    }
                }
            }

            // ==========================================
            // 2. LEER CREDENCIALES (DB con prioridad absoluta, fallback a IConfiguration)
            // ==========================================
            var host = ObtenerValor(configDict, ConfiguracionKeys.EmailHost, "SMTP_HOST", "Email:Host", "Host", "EmailHost", "BREVO_HOST", "BREVO_SMTP_HOST");
            var portString = ObtenerValor(configDict, ConfiguracionKeys.EmailPort, "SMTP_PORT", "Email:Port", "Port", "EmailPort", "BREVO_PORT", "BREVO_SMTP_PORT");
            var user = ObtenerValor(configDict, ConfiguracionKeys.EmailUser, "SMTP_USER", "Email:User", "User", "EmailUser", "Usuario", "BREVO_USER", "BREVO_LOGIN", "BREVO_EMAIL");
            var key = ObtenerValor(configDict, ConfiguracionKeys.EmailKey, "EMAIL_PASSWORD", "EMAIL_PASS", "SMTP_KEY", "SMTP_PASSWORD", "SMTP_PASS", "Email:Key", "Email:Password", "Password", "BREVO_KEY", "BREVO_API_KEY", "BREVO_SMTP_KEY", "BREVO_PASSWORD", "BREVO_PASS");
            var from = ObtenerValor(configDict, ConfiguracionKeys.EmailFrom, "SMTP_FROM", "Email:From", "From", "EmailFrom", "FromEmail", "EmailSettingsSendGrid:FromEmail", "BREVO_FROM", "BREVO_EMAIL_FROM");
            var fromNombre = ObtenerValor(configDict, ConfiguracionKeys.EmailFromNombre, "SMTP_FROM_NOMBRE", "Email:FromNombre", "Email:FromName", "FromName", "EmailSettingsSendGrid:FromName", "BREVO_FROM_NOMBRE", "BREVO_FROM_NAME");
            var brevoApiKey = ObtenerValor(configDict, "BREVO_API_KEY", "BREVO_KEY", "EMAIL_BREVO_KEY", "Brevo:ApiKey", "EmailSettingsBrevo:ApiKey");
            var sendGridApiKey = ObtenerValor(configDict, "SENDGRID_API_KEY", "SENDGRID_KEY", "EMAIL_SENDGRID_KEY", "SendGrid:ApiKey", "EmailSettingsSendGrid:ApiKey");

            if (string.IsNullOrWhiteSpace(fromNombre))
            {
                fromNombre = "AdLocal";
            }

            // Si "From" está vacío, usar "User" si contiene formato de correo
            if (string.IsNullOrWhiteSpace(from))
            {
                if (!string.IsNullOrWhiteSpace(user) && user.Contains('@'))
                {
                    from = user;
                }
            }

            // ==========================================
            // 3. LIMPIEZA Y DETECCIÓN INTELIGENTE DE PROVEEDOR (BREVO / GMAIL / SENDGRID)
            // ==========================================
            host = host?.Trim() ?? string.Empty;
            user = user?.Trim() ?? string.Empty;
            from = from?.Trim() ?? string.Empty;
            key = key?.Trim() ?? string.Empty;

            var esBrevo = host.Contains("brevo", StringComparison.OrdinalIgnoreCase) ||
                          host.Contains("sendinblue", StringComparison.OrdinalIgnoreCase) ||
                          key.StartsWith("xsmtpsib-", StringComparison.OrdinalIgnoreCase) ||
                          key.StartsWith("xkeysib-", StringComparison.OrdinalIgnoreCase) ||
                          user.Contains("brevo", StringComparison.OrdinalIgnoreCase) ||
                          user.Contains("sendinblue", StringComparison.OrdinalIgnoreCase) ||
                          !string.IsNullOrWhiteSpace(brevoApiKey);

            if (esBrevo && string.IsNullOrWhiteSpace(host))
            {
                host = "smtp-relay.brevo.com";
            }

            // Si la clave tiene espacios (contraseñas de aplicación de Google "xxxx xxxx xxxx xxxx")
            if (!string.IsNullOrWhiteSpace(key))
            {
                if (host.Contains("gmail", StringComparison.OrdinalIgnoreCase) || key.Contains(' '))
                {
                    key = key.Replace(" ", "");
                }
            }

            if (!int.TryParse(portString, out var port) || port <= 0 || port > 65535)
            {
                port = 587;
            }

            // ==========================================
            // 4. SI SE DETECTA CLAVE REST API DE BREVO (xkeysib-), INTENTAR PRIMERO VÍA HTTPS 443
            //    (Inmune a bloqueos de puertos SMTP 587/465 por ISPs y firewalls)
            // ==========================================
            var apiKeyParaBrevo = !string.IsNullOrWhiteSpace(brevoApiKey)
                ? brevoApiKey
                : (key.StartsWith("xkeysib-", StringComparison.OrdinalIgnoreCase) ? key : string.Empty);

            if (!string.IsNullOrWhiteSpace(apiKeyParaBrevo))
            {
                var remitente = !string.IsNullOrWhiteSpace(from) ? from : user;
                if (!string.IsNullOrWhiteSpace(remitente))
                {
                    var envioExitoso = await IntentarEnvioBrevoApiAsync(
                        apiKeyParaBrevo,
                        remitente,
                        fromNombre,
                        para,
                        asunto,
                        htmlContenido
                    );

                    if (envioExitoso)
                    {
                        return; // Enviado exitosamente vía Brevo HTTP REST API
                    }
                }
            }

            // ==========================================
            // 5. ENVÍO VÍA SMTP (BREVO RELAY, GMAIL, OUTLOOK, ETC.) CON TIMEOUT Y PUERTOS DINÁMICOS
            // ==========================================
            Exception? smtpUltimaExcepcion = null;

            if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(from))
            {
                var puertosAIntentar = DeterminarPuertosAIntentar(host, port);

                foreach (var (puertoIntento, opcionesSocket) in puertosAIntentar)
                {
                    try
                    {
                        _logger.LogInformation("Intentando envío SMTP a {Host}:{Port} ({Options}) para destinatario {Para}...", host, puertoIntento, opcionesSocket, para);

                        using var smtp = new SmtpClient();
                        smtp.Timeout = 8000; // 8 segundos para evitar bloqueos largos de interfaz
                        smtp.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                        await smtp.ConnectAsync(host, puertoIntento, opcionesSocket);
                        await smtp.AuthenticateAsync(user, key);

                        var mensaje = new MimeMessage();
                        mensaje.From.Add(new MailboxAddress(fromNombre, from));
                        mensaje.To.Add(MailboxAddress.Parse(para.Trim()));
                        mensaje.Subject = asunto ?? "Notificación - AdLocal";

                        var bodyBuilder = new BodyBuilder { HtmlBody = htmlContenido };
                        mensaje.Body = bodyBuilder.ToMessageBody();

                        await smtp.SendAsync(mensaje);
                        await smtp.DisconnectAsync(true);

                        _logger.LogInformation("Correo enviado exitosamente vía SMTP ({Host}:{Port}) a {Para}.", host, puertoIntento, para);
                        return; // Éxito inmediato
                    }
                    catch (Exception ex)
                    {
                        smtpUltimaExcepcion = ex;
                        _logger.LogWarning(ex, "Intento SMTP fallido en {Host}:{Port} ({Message}). Evaluando reintento o fallback...", host, puertoIntento, ex.Message);
                    }
                }
            }

            // ==========================================
            // 6. FALLBACK A BREVO REST API (SI FALLÓ SMTP Y HAY API KEY)
            // ==========================================
            if (!string.IsNullOrWhiteSpace(apiKeyParaBrevo))
            {
                var remitente = !string.IsNullOrWhiteSpace(from) ? from : user;
                if (!string.IsNullOrWhiteSpace(remitente))
                {
                    var exitoBrevo = await IntentarEnvioBrevoApiAsync(
                        apiKeyParaBrevo,
                        remitente,
                        fromNombre,
                        para,
                        asunto,
                        htmlContenido
                    );
                    if (exitoBrevo) return;
                }
            }

            // ==========================================
            // 7. FALLBACK A SENDGRID REST API (HTTPS 443)
            // ==========================================
            var apiKeyParaSendGrid = !string.IsNullOrWhiteSpace(sendGridApiKey)
                ? sendGridApiKey
                : (key.StartsWith("SG.", StringComparison.OrdinalIgnoreCase) ? key : string.Empty);

            if (!string.IsNullOrWhiteSpace(apiKeyParaSendGrid))
            {
                try
                {
                    _logger.LogInformation("Activando envío de correo vía SendGrid HTTP API (HTTPS 443) para {Para}...", para);

                    var remitente = !string.IsNullOrWhiteSpace(from) ? from : "no-reply@adlocal.mx";
                    var client = new SendGridClient(apiKeyParaSendGrid);
                    var msg = MailHelper.CreateSingleEmail(
                        new EmailAddress(remitente, fromNombre),
                        new EmailAddress(para.Trim()),
                        asunto ?? "Notificación - AdLocal",
                        plainTextContent: null,
                        htmlContent: htmlContenido
                    );

                    var response = await client.SendEmailAsync(msg);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Correo enviado exitosamente a {Para} vía SendGrid HTTP API.", para);
                        return;
                    }

                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogWarning("SendGrid API respondió con status {StatusCode}: {Body}", response.StatusCode, responseBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar correo vía SendGrid HTTP API a {Para}", para);
                }
            }

            // ==========================================
            // 8. MANEJO FINAL DE ERROR SI NINGÚN MÉTODO FUNCIONÓ
            // ==========================================
            if (smtpUltimaExcepcion != null)
            {
                var ayudaBrevo = esBrevo
                    ? $" (Diagnóstico Brevo: asegúrese de que el remitente '{from}' esté validado en su cuenta de Brevo > Senders & IP > Senders. Para SMTP la clave suele comenzar con 'xsmtpsib-...', o si usa la API HTTP 'xkeysib-...')"
                    : string.Empty;

                throw new InvalidOperationException(
                    $"No fue posible enviar el correo vía SMTP ({host}:{port}). Detalle: {smtpUltimaExcepcion.Message}{ayudaBrevo}",
                    smtpUltimaExcepcion
                );
            }

            throw new InvalidOperationException(
                "No se han configurado credenciales válidas de correo electrónico (EMAIL_HOST, EMAIL_USER, EMAIL_KEY, EMAIL_FROM) en la base de datos."
            );
        }

        private async Task<bool> IntentarEnvioBrevoApiAsync(
            string apiKey,
            string remitenteEmail,
            string remitenteNombre,
            string destinatarioEmail,
            string asunto,
            string htmlContenido)
        {
            try
            {
                _logger.LogInformation("Intentando envío de correo vía Brevo HTTP API (HTTPS 443) para {Para}...", destinatarioEmail);

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
                request.Headers.Add("api-key", apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new
                {
                    sender = new { name = remitenteNombre, email = remitenteEmail },
                    to = new[] { new { email = destinatarioEmail } },
                    subject = asunto ?? "Notificación - AdLocal",
                    htmlContent = htmlContenido
                };

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var client = _httpClientFactory?.CreateClient() ?? new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Correo enviado exitosamente a {Para} vía Brevo HTTP API.", destinatarioEmail);
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Brevo HTTP API respondió con código {StatusCode}: {Error}", response.StatusCode, errorContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al enviar correo vía Brevo HTTP API a {Para}: {Message}", destinatarioEmail, ex.Message);
                return false;
            }
        }

        private static List<(int Puerto, SecureSocketOptions Opciones)> DeterminarPuertosAIntentar(string host, int puertoConfigurado)
        {
            var resultado = new List<(int Puerto, SecureSocketOptions Opciones)>();

            // Puerto principal según lo configurado
            var opcionPrincipal = puertoConfigurado switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                2525 => SecureSocketOptions.StartTls,
                25 => SecureSocketOptions.StartTlsWhenAvailable,
                _ => SecureSocketOptions.Auto
            };
            resultado.Add((puertoConfigurado, opcionPrincipal));

            var esBrevo = host.Contains("brevo", StringComparison.OrdinalIgnoreCase) ||
                          host.Contains("sendinblue", StringComparison.OrdinalIgnoreCase);

            var esSendGrid = host.Contains("sendgrid", StringComparison.OrdinalIgnoreCase);

            // Si es 587, agregar 465 con SSL como fallback
            if (puertoConfigurado == 587)
            {
                resultado.Add((465, SecureSocketOptions.SslOnConnect));
            }
            // Si es 465, agregar 587 con STARTTLS como fallback
            else if (puertoConfigurado == 465)
            {
                resultado.Add((587, SecureSocketOptions.StartTls));
            }

            // Brevo soporta oficialmente 587, 465 y 2525 (puerto alternativo muy útil si 587 está filtrado por el ISP)
            if (esBrevo)
            {
                if (puertoConfigurado != 2525 && !resultado.Exists(p => p.Puerto == 2525))
                {
                    resultado.Add((2525, SecureSocketOptions.StartTls));
                }
                if (puertoConfigurado != 587 && !resultado.Exists(p => p.Puerto == 587))
                {
                    resultado.Add((587, SecureSocketOptions.StartTls));
                }
                if (puertoConfigurado != 465 && !resultado.Exists(p => p.Puerto == 465))
                {
                    resultado.Add((465, SecureSocketOptions.SslOnConnect));
                }
            }

            // Si es SendGrid SMTP, soporta también 2525
            if (esSendGrid && puertoConfigurado != 2525)
            {
                resultado.Add((2525, SecureSocketOptions.StartTls));
            }

            return resultado;
        }

        private string ObtenerValor(
            Dictionary<string, string> dict,
            string dbKey,
            params string[] fallbackKeys)
        {
            // 1. Clave exacta en diccionario
            if (dict.TryGetValue(dbKey, out var val) && !string.IsNullOrWhiteSpace(val))
            {
                return val.Trim();
            }

            // 2. Probar claves de fallback directamente contra el diccionario
            foreach (var key in fallbackKeys)
            {
                if (dict.TryGetValue(key, out var altVal) && !string.IsNullOrWhiteSpace(altVal))
                {
                    return altVal.Trim();
                }

                var clean = key.Replace("Email:", "", StringComparison.OrdinalIgnoreCase)
                               .Replace("SMTP_", "", StringComparison.OrdinalIgnoreCase)
                               .Replace("EmailSettingsSendGrid:", "", StringComparison.OrdinalIgnoreCase)
                               .Replace("BREVO_", "", StringComparison.OrdinalIgnoreCase);

                if (dict.TryGetValue(clean, out var cleanVal) && !string.IsNullOrWhiteSpace(cleanVal))
                {
                    return cleanVal.Trim();
                }
            }

            // 3. Fallback a IConfiguration (appsettings.Local.json / variables de entorno)
            if (_config != null)
            {
                var cfgVal = _config[dbKey];
                if (!string.IsNullOrWhiteSpace(cfgVal)) return cfgVal.Trim();

                foreach (var k in fallbackKeys)
                {
                    cfgVal = _config[k];
                    if (!string.IsNullOrWhiteSpace(cfgVal)) return cfgVal.Trim();
                }
            }

            return string.Empty;
        }
    }
}