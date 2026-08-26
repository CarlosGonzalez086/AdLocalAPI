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
                    (
                        "Clave pública de Stripe utilizada en el frontend",
                        "STRING"
                    )
                },
                {
                    ConfiguracionKeys.StripeSecretKey,
                    (
                        "Clave secreta de Stripe utilizada en el backend",
                        "STRING"
                    )
                },
                {
                    ConfiguracionKeys.StripeCommissionPercentage,
                    (
                        "Comisión porcentual que cobra Stripe por transacción",
                        "DECIMAL"
                    )
                },
                {
                    ConfiguracionKeys.StripeCommissionFixed,
                    (
                        "Comisión fija (neta) que cobra Stripe por transacción",
                        "DECIMAL"
                    )
                },
                {
                    ConfiguracionKeys.Ip2LocationKey,
                    (
                        "IP2Location es una solución de geolocalización de IP que permite identificar la ubicación geográfica de los visitantes de un sitio web mediante su dirección IP",
                        "STRING"
                    )
                },

                // ==========================================
                // ADLOCAL / MARKETPLACE
                // ==========================================

                {
                    ConfiguracionKeys.MarketplaceCommissionPercentage,
                    (
                        "Porcentaje de comisión que cobra ADLocal por cada pedido",
                        "DECIMAL"
                    )
                },
                {
                    ConfiguracionKeys.MarketplaceCommissionFixed,
                    (
                        "Monto fijo adicional que cobra ADLocal por cada pedido",
                        "DECIMAL"
                    )
                },
                {
                    ConfiguracionKeys.MarketplaceCommissionEnabled,
                    (
                        "Indica si la comisión de ADLocal se encuentra activa",
                        "BOOLEAN"
                    )
                },
                 // ==========================================
                // ADLOCAL / CORREO CONFIGURACIONES
                // ==========================================
                          {
                    ConfiguracionKeys.EmailHost,
                    (
                     "Servidor SMTP utilizado para enviar correos",
                     "string"
                    )
                },
                {
                    ConfiguracionKeys.EmailPort,
                    (
                        "Monto fijo adicional que cobra ADLocal por cada pedido",
                        "DECIMAL"
                    )
                },
                {
                    ConfiguracionKeys.EmailUser,
                    (
                        "Indica si la comisión de ADLocal se encuentra activa",
                        "BOOLEAN"
                    )
                },
                          {
                    ConfiguracionKeys.EmailKey,
                    (
                     "Servidor SMTP utilizado para enviar correos",
                     "string"
                    )
                },
                {
                    ConfiguracionKeys.EmailFrom,
                    (
                        "Monto fijo adicional que cobra ADLocal por cada pedido",
                        "DECIMAL"
                    )
                },
                {
                    ConfiguracionKeys.EmailFromNombre,
                    (
                        "Indica si la comisión de ADLocal se encuentra activa",
                        "BOOLEAN"
                    )
                },
            };
    }
}