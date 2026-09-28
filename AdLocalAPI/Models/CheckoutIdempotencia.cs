using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("checkout_idempotencias")]
    public class CheckoutIdempotencia
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string IdempotencyKey { get; set; } = string.Empty;

        [Required]
        public long IdUsuario { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "processing"; // processing, completed, failed

        public string? ResponseJson { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaCompletado { get; set; }
    }
}
