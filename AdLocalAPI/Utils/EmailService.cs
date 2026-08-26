using AdLocalAPI.Constants;
using AdLocalAPI.Interfaces;
using System.Net;
using System.Net.Mail;

namespace AdLocalAPI.Utils
{
    public class EmailService
    {
        private readonly IConfiguracionRepository _repository;

        public EmailService(
            IConfiguracionRepository repository)
        {
            _repository = repository;
        }

        public async Task EnviarCorreoAsync(
            string para,
            string asunto,
            string htmlContenido)
        {
            // ==========================================
            // OBTENER CONFIGURACIÓN
            // ==========================================

            var configuraciones =
                await _repository.ObtenerTodosAsync();

            var config = configuraciones
                .ToDictionary(
                    x => x.Key,
                    x => x.Val ?? string.Empty
                );

            // ==========================================
            // LEER CONFIGURACIÓN SMTP
            // ==========================================

            var host = ObtenerValor(
                config,
                ConfiguracionKeys.EmailHost
            );

            var portString = ObtenerValor(
                config,
                ConfiguracionKeys.EmailPort
            );

            var user = ObtenerValor(
                config,
                ConfiguracionKeys.EmailUser
            );

            var key = ObtenerValor(
                config,
                ConfiguracionKeys.EmailKey
            );

            var from = ObtenerValor(
                config,
                ConfiguracionKeys.EmailFrom
            );

            var fromNombre = ObtenerValor(
                config,
                ConfiguracionKeys.EmailFromNombre,
                "ADLocal"
            );

            // ==========================================
            // VALIDACIONES
            // ==========================================

            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException(
                    "No se ha configurado el servidor SMTP."
                );
            }

            if (!int.TryParse(portString, out var port))
            {
                throw new InvalidOperationException(
                    "El puerto SMTP configurado no es válido."
                );
            }

            if (port <= 0 || port > 65535)
            {
                throw new InvalidOperationException(
                    "El puerto SMTP configurado no es válido."
                );
            }

            if (string.IsNullOrWhiteSpace(user))
            {
                throw new InvalidOperationException(
                    "No se ha configurado el usuario SMTP."
                );
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException(
                    "No se ha configurado la clave SMTP."
                );
            }

            if (string.IsNullOrWhiteSpace(from))
            {
                throw new InvalidOperationException(
                    "No se ha configurado el correo remitente."
                );
            }

            if (string.IsNullOrWhiteSpace(para))
            {
                throw new ArgumentException(
                    "El correo destinatario es requerido.",
                    nameof(para)
                );
            }

            // ==========================================
            // CREAR MENSAJE
            // ==========================================

            using var mensaje = new MailMessage
            {
                From = new MailAddress(
                    from,
                    fromNombre
                ),

                Subject = asunto,

                Body = htmlContenido,

                IsBodyHtml = true
            };

            mensaje.To.Add(
                new MailAddress(para)
            );

            // ==========================================
            // CONFIGURAR SMTP
            // ==========================================

            using var smtp = new SmtpClient(
                host,
                port
            )
            {
                EnableSsl = true,

                UseDefaultCredentials = false,

                DeliveryMethod =
                    SmtpDeliveryMethod.Network,

                Credentials =
                    new NetworkCredential(
                        user,
                        key
                    )
            };

            // ==========================================
            // ENVIAR
            // ==========================================

            await smtp.SendMailAsync(
                mensaje
            );
        }

        private static string ObtenerValor(
            Dictionary<string, string> configuraciones,
            string key,
            string defaultValue = "")
        {
            if (
                configuraciones.TryGetValue(
                    key,
                    out var value
                ) &&
                !string.IsNullOrWhiteSpace(value)
            )
            {
                return value;
            }

            return defaultValue;
        }
    }
}