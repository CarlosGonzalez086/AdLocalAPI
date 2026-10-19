using System;
using System.Collections.Generic;
using System.Net.Sockets;
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

        public EmailService(
            IConfiguracionRepository repository,
            IConfiguration config,
            ILogger<EmailService> logger)
        {
            _repository = repository;
            _config = config;
            _logger = logger;
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
            var host = ObtenerValor(configDict, ConfiguracionKeys.EmailHost, "SMTP_HOST", "Email:Host", "Host", "EmailHost");
            var portString = ObtenerValor(configDict, ConfiguracionKeys.EmailPort, "SMTP_PORT", "Email:Port", "Port", "EmailPort");
            var user = ObtenerValor(configDict, ConfiguracionKeys.EmailUser, "SMTP_USER", "Email:User", "User", "EmailUser", "Usuario");
            var key = ObtenerValor(configDict, ConfiguracionKeys.EmailKey, "EMAIL_PASSWORD", "EMAIL_PASS", "SMTP_KEY", "SMTP_PASSWORD", "SMTP_PASS", "Email:Key", "Email:Password", "Password");
            var from = ObtenerValor(configDict, ConfiguracionKeys.EmailFrom, "SMTP_FROM", "Email:From", "From", "EmailFrom", "FromEmail", "EmailSettingsSendGrid:FromEmail");
            var fromNombre = ObtenerValor(configDict, ConfiguracionKeys.EmailFromNombre, "SMTP_FROM_NOMBRE", "Email:FromNombre", "Email:FromName", "FromName", "EmailSettingsSendGrid:FromName");
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
            // 3. LIMPIEZA Y NORMALIZACIÓN DE CREDENCIALES
            // ==========================================
            host = host?.Trim() ?? string.Empty;
            user = user?.Trim() ?? string.Empty;
            from = from?.Trim() ?? string.Empty;

            // Si la clave tiene espacios (contraseñas de aplicación de Google "xxxx xxxx xxxx xxxx")
            if (!string.IsNullOrWhiteSpace(key))
            {
                key = key.Trim();
                if (host.Contains("gmail", StringComparison.OrdinalIgnoreCase) || key.Contains(' '))
                {
                    key = key.Replace(" ", "");
                }
            }

            if (!int.TryParse(portString, out var port) || port <= 0 || port > 65535)
            {
                port = host.Contains("gmail", StringComparison.OrdinalIgnoreCase) ? 587 : 587;
            }

            // ==========================================
            // 4. ENVÍO VÍA SMTP (CON TIMEOUT CONTROLADO Y AUTO-FALLBACK DE PUERTO)
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
                        smtp.Timeout = 8000; // 8 segundos para evitar bloqueos largos
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
            // 5. FALLBACK AUTOMÁTICO A SENDGRID HTTP API (PUERTO 443 HTTPS NUNCA SE BLOQUEA)
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
            // 6. MANEJO FINAL DE ERROR SI NINGÚN MÉTODO FUNCIONÓ
            // ==========================================
            if (smtpUltimaExcepcion != null)
            {
                throw new InvalidOperationException(
                    $"No fue posible enviar el correo vía SMTP ({host}:{port}). Detalle: {smtpUltimaExcepcion.Message}",
                    smtpUltimaExcepcion
                );
            }

            throw new InvalidOperationException(
                "No se han configurado credenciales válidas de correo electrónico (EMAIL_HOST, EMAIL_USER, EMAIL_KEY, EMAIL_FROM) en la base de datos."
            );
        }

        private static List<(int Puerto, SecureSocketOptions Opciones)> DeterminarPuertosAIntentar(string host, int puertoConfigurado)
        {
            var resultado = new List<(int Puerto, SecureSocketOptions Opciones)>();

            // Puerto principal según lo configurado
            var opcionPrincipal = puertoConfigurado switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                25 => SecureSocketOptions.StartTlsWhenAvailable,
                _ => SecureSocketOptions.Auto
            };
            resultado.Add((puertoConfigurado, opcionPrincipal));

            // Si es 587, agregar 465 con SSL como fallback (común cuando los ISPs bloquean 587)
            if (puertoConfigurado == 587)
            {
                resultado.Add((465, SecureSocketOptions.SslOnConnect));
            }
            // Si es 465, agregar 587 con STARTTLS como fallback
            else if (puertoConfigurado == 465)
            {
                resultado.Add((587, SecureSocketOptions.StartTls));
            }

            // Si es SendGrid SMTP, soporta también 2525
            if (host.Contains("sendgrid", StringComparison.OrdinalIgnoreCase))
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
                               .Replace("EmailSettingsSendGrid:", "", StringComparison.OrdinalIgnoreCase);

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