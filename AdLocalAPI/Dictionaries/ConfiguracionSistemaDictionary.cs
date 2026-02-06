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
                },
                {
                    ConfiguracionKeys.Ip2LocationKey,
                    ("IP2Location es una solución de geolocalización de IP que permite identificar la ubicación geográfica de los visitantes de un sitio web mediante su dirección IP", "STRING")
                }
            };
    }
}
