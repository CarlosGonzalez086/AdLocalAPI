namespace AdLocalAPI.DTOs
{
    public class StripeConfiguracionDto
    {
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string CommissionPercentage { get; set; } = string.Empty;
        public string CommissionFixed { get; set; } = string.Empty;
    }
}
