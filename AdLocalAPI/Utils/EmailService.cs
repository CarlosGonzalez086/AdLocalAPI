using AdLocalAPI.Constants;
using AdLocalAPI.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace AdLocalAPI.Utils
{
    public class EmailService
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
            // 1. OBTENER CONFIGURACIÓN DE BASE DE DATOS
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
            // 2. LEER CONFIGURACIÓN SMTP (DB con fallback a IConfiguration)
            // ==========================================
            var host = ObtenerValor(configDict, ConfiguracionKeys.EmailHost, "Email:Host", "SMTP_HOST");
            var portString = ObtenerValor(configDict, ConfiguracionKeys.EmailPort, "Email:Port", "SMTP_PORT");
            var user = ObtenerValor(configDict, ConfiguracionKeys.EmailUser, "Email:User", "SMTP_USER");
            var key = ObtenerValor(configDict, ConfiguracionKeys.EmailKey, "Email:Key", "SMTP_KEY", "Email:Password", "SMTP_PASSWORD");
            var from = ObtenerValor(configDict, ConfiguracionKeys.EmailFrom, "Email:From", "SMTP_FROM");
            var fromNombre = ObtenerValor(configDict, ConfiguracionKeys.EmailFromNombre, "Email:FromNombre", "Email:FromName", "SMTP_FROM_NOMBRE");

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
            host = host.Trim();
            user = user.Trim();
            from = from.Trim();

            // Si la clave tiene espacios (común al copiar contraseñas de aplicación de Google "xxxx xxxx xxxx xxxx")
            if (!string.IsNullOrWhiteSpace(key))
            {
                key = key.Trim();
                if (host.Contains("gmail", StringComparison.OrdinalIgnoreCase) || key.Contains(' '))
                {
                    key = key.Replace(" ", "");
                }
            }

            // ==========================================
            // 4. VALIDACIONES DE CONFIGURACIÓN
            // ==========================================
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException("No se ha configurado el servidor SMTP (EMAIL_HOST) en la base de datos.");
            }

            if (!int.TryParse(portString, out var port) || port <= 0 || port > 65535)
            {
                port = host.Contains("gmail", StringComparison.OrdinalIgnoreCase) ? 587 : 587;
                _logger.LogWarning("Puerto SMTP no especificado o inválido '{PortString}'. Usando puerto por defecto: {Port}", portString, port);
            }

            if (string.IsNullOrWhiteSpace(user))
            {
                throw new InvalidOperationException("No se ha configurado el usuario SMTP (EMAIL_USER) en la base de datos.");
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("No se ha configurado la clave SMTP (EMAIL_KEY) en la base de datos.");
            }

            if (string.IsNullOrWhiteSpace(from))
            {
                throw new InvalidOperationException("No se ha configurado el correo remitente (EMAIL_FROM) ni un usuario SMTP válido.");
            }

            // ==========================================
            // 5. CONSTRUCCIÓN DEL MENSAJE (MimeKit)
            // ==========================================
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(fromNombre, from));
            mensaje.To.Add(MailboxAddress.Parse(para.Trim()));
            mensaje.Subject = asunto ?? "Notificación - AdLocal";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlContenido
            };
            mensaje.Body = bodyBuilder.ToMessageBody();

            // ==========================================
            // 6. DETECCIÓN DE MODO SSL / TLS SEGÚN PUERTO
            // ==========================================
            SecureSocketOptions socketOptions = port switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                25 => SecureSocketOptions.StartTlsWhenAvailable,
                _ => SecureSocketOptions.Auto
            };

            // ==========================================
            // 7. ENVÍO VÍA MAILKIT
            // ==========================================
            using var smtp = new SmtpClient();
            smtp.Timeout = 15000; // 15 segundos timeout

            // Evitar bloqueos por certificados intermedios
            smtp.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            try
            {
                _logger.LogInformation("Conectando al servidor SMTP {Host}:{Port} ({Options})...", host, port, socketOptions);
                await smtp.ConnectAsync(host, port, socketOptions);

                _logger.LogInformation("Autenticando usuario SMTP: {User}...", user);
                await smtp.AuthenticateAsync(user, key);

                _logger.LogInformation("Enviando correo a {Para} con asunto: {Asunto}...", para, asunto);
                await smtp.SendAsync(mensaje);

                await smtp.DisconnectAsync(true);
                _logger.LogInformation("Correo enviado exitosamente a {Para}.", para);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _logger.LogError(ex, "Error de autenticación SMTP al conectar con {User}@{Host}:{Port}", user, host, port);
                throw new InvalidOperationException(
                    $"Error de autenticación SMTP con el usuario '{user}'. Verifica que la clave o 'Contraseña de Aplicación' de Google sea correcta y esté activa. Detalle: {ex.Message}",
                    ex
                );
            }
            catch (MailKit.Net.Smtp.SmtpCommandException ex)
            {
                _logger.LogError(ex, "Error en comando SMTP ({StatusCode}) con {Host}:{Port}", ex.StatusCode, host, port);
                throw new InvalidOperationException(
                    $"El servidor SMTP rechazó el comando (Código {ex.StatusCode}): {ex.Message}",
                    ex
                );
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _logger.LogError(ex, "No se pudo establecer conexión de red con el servidor SMTP {Host}:{Port}", host, port);
                throw new InvalidOperationException(
                    $"No se pudo conectar al servidor SMTP '{host}:{port}'. Revisa el nombre del host, el puerto o si las reglas de red/firewall bloquean la conexión saliente. Detalle: {ex.Message}",
                    ex
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo a {Para} vía {Host}:{Port}", para, host, port);
                throw new InvalidOperationException(
                    $"Ocurrió un error al enviar el correo: {ex.Message}",
                    ex
                );
            }
        }

        private string ObtenerValor(
            Dictionary<string, string> dict,
            string dbKey,
            params string[] fallbackConfigKeys)
        {
            // 1. Intentar desde base de datos con clave directa
            if (dict.TryGetValue(dbKey, out var val) && !string.IsNullOrWhiteSpace(val))
            {
                return val;
            }

            // 2. Intentar buscar en el diccionario ignorando prefijos comunes
            foreach (var key in fallbackConfigKeys)
            {
                var clean = key.Replace("Email:", "").Replace("SMTP_", "");
                if (dict.TryGetValue(clean, out var altVal) && !string.IsNullOrWhiteSpace(altVal))
                {
                    return altVal;
                }
            }

            // 3. Fallback a IConfiguration (appsettings.json / variables de entorno)
            if (_config != null)
            {
                var cfgVal = _config[dbKey];
                if (!string.IsNullOrWhiteSpace(cfgVal)) return cfgVal;

                foreach (var k in fallbackConfigKeys)
                {
                    cfgVal = _config[k];
                    if (!string.IsNullOrWhiteSpace(cfgVal)) return cfgVal;
                }
            }

            return string.Empty;
        }
    }
}