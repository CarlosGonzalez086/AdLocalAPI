namespace AdLocalAPI.Utils
{
    public class StripeSettings
    {
        public string SecretKey { get; private set; }
        public string WebhookSecret { get; private set; }
        public void Inicializar(string secretKey, string webhookSecret)
        {
            SecretKey = secretKey;
            WebhookSecret = webhookSecret;
        }
    }
}
