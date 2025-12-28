using AdLocalAPI.Constants;

namespace AdLocalAPI.Dictionaries
{
    public static class ConfiguracionSistemaDictionary
    {
        public static readonly Dictionary<string, (string Description, string Tipo)> Data =
            new()
            {
                {
                    ConfiguracionKeys.StripePublishableKey,
                    ("Clave pública de Stripe utilizada en el frontend", "STRING")
                },
                {
                    ConfiguracionKeys.StripeSecretKey,
                    ("Clave secreta de Stripe utilizada en el backend", "STRING")
                },
                {
                    ConfiguracionKeys.StripeCommissionPercentage,
                    ("Comisión porcentual que cobra Stripe por transacción", "DECIMAL")
                },
                {
                    ConfiguracionKeys.StripeCommissionFixed,
                    ("Comisión fija (neta) que cobra Stripe por transacción", "DECIMAL")
                }
            };
    }
}
