using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AdLocalAPI.Utils
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task EnviarCorreoAsync(
            string para,
            string asunto,
            string htmlContenido)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("AdLocal", _settings.User));
            email.To.Add(MailboxAddress.Parse(para));
            email.Subject = asunto;

            email.Body = new BodyBuilder
            {
                HtmlBody = htmlContenido
            }.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.User, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
