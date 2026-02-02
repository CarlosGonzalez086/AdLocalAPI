namespace AdLocalAPI.Utils
{
    public class StripeSettings
    {
        public string SecretKey { get; private set; }

        public void Inicializar(string secretKey)
        {
            SecretKey = secretKey;

        }
    }
}
