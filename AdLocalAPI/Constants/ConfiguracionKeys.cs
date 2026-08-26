namespace AdLocalAPI.Constants
{
    public static class ConfiguracionKeys
    {
        public const string StripePublishableKey = "STRIPE_PUBLISHABLE_KEY";
        public const string StripeSecretKey = "STRIPE_SECRET_KEY";

        public const string StripeCommissionPercentage = "STRIPE_COMMISSION_PERCENTAGE";
        public const string StripeCommissionFixed = "STRIPE_COMMISSION_FIXED";
        public const string Ip2LocationKey = "IP2LOCATION_KEY";
        // =========================
        // ADLOCAL / MARKETPLACE
        // =========================

        public const string MarketplaceCommissionPercentage =
            "MARKETPLACE_COMMISSION_PERCENTAGE";

        public const string MarketplaceCommissionFixed =
            "MARKETPLACE_COMMISSION_FIXED";

        public const string MarketplaceCommissionEnabled =
            "MARKETPLACE_COMMISSION_ENABLED";
        // EMAIL
        public const string EmailHost =
            "EMAIL_HOST";

        public const string EmailPort =
            "EMAIL_PORT";

        public const string EmailUser =
            "EMAIL_USER";

        public const string EmailKey =
            "EMAIL_KEY";

        public const string EmailFrom =
            "EMAIL_FROM";

        public const string EmailFromNombre =
            "EMAIL_FROM_NOMBRE";
    }
}
