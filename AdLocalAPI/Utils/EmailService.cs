using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AdLocalAPI.Utils
{
    public class EmailService
    {
        private readonly EmailSettingsSendGrid _settings;

        public EmailService(IOptions<EmailSettingsSendGrid> settings)
        {
            _settings = settings.Value;
        }

        public async Task EnviarCorreoAsync(string para, string asunto, string htmlContenido)
        {
            Console.WriteLine(_settings.ApiKey);
            var client = new SendGridClient(_settings.ApiKey);
            Console.WriteLine(client);
            var msg = MailHelper.CreateSingleEmail(
                from: new EmailAddress(_settings.FromEmail, _settings.FromName),
                to: new EmailAddress(para),
                subject: asunto,
                plainTextContent: null,
                htmlContent: htmlContenido
            );

            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Body.ReadAsStringAsync();
                throw new Exception($"Error al enviar correo: {response.StatusCode} - {body}");
            }
        }
    }
}