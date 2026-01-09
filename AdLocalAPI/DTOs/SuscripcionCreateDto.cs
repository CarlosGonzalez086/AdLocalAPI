namespace AdLocalAPI.DTOs
{
    public class SuscripcionCreateDto
    {
        public int PlanId { get; set; }
        public string StripePaymentMethodId { get; set; } = string.Empty;
    }
}
