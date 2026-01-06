namespace AdLocalAPI.DTOs
{
    public class TarjetaDto
    {
        public long Id { get; set; }
        public string Brand { get; set; } = null!;
        public string Last4 { get; set; } = null!;
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }
        public string CardType { get; set; } = null!;
        public bool IsDefault { get; set; }
        public string StripePaymentMethodId { get; set; }
    }
}
