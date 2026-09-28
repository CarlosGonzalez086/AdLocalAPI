using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("StripeWebhookEvents")]
    public class StripeWebhookEvent
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string StripeEventId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "processed"; // processed, failed, ignored

        public string? ErrorMessage { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }
    }
}
