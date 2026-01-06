namespace AdLocalAPI.Models
{
    public class Tarjeta
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string StripeCustomerId { get; set; } = null!;
        public string StripePaymentMethodId { get; set; } = null!;
        public string Brand { get; set; } = null!;    
        public string Last4 { get; set; } = null!;  
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }
        public string CardType { get; set; } = null!;
        public bool IsDefault { get; set; } = false;
        public bool Status { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
    }
}
