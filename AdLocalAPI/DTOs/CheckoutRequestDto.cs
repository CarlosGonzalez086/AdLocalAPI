namespace AdLocalAPI.DTOs
{
    public class CheckoutRequestDto
    {
        public int PlanId { get; set; }
        public string Metodo { get; set; }
        public string StripePaymentMethodId { get; set; } = "";
        public string banco { get; set; } = "";
        public bool autoRenew { get; set; } = false;
    }
    public class CambiarPlanDto
    {
        public int PlanIdNuevo { get; set; }
        public string Metodo { get; set; } = "";
        public string? StripePaymentMethodId { get; set; }
    }

}
