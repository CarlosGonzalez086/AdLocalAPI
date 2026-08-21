namespace AdLocalAPI.DTOs
{
    public class StripeConfiguracionDto
    {
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string CommissionPercentage { get; set; } = string.Empty;
        public string CommissionFixed { get; set; } = string.Empty;
    }
    public class ClavesConfigDto
    {
        public string Ip2LocationKey { get; set; } = string.Empty;

    }
    public class ComisionMarketplaceDto
    {
        public decimal Porcentaje { get; set; }

        public decimal MontoFijo { get; set; } = 0;

        public bool Activa { get; set; } = true;
    }
}
