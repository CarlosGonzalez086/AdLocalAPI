using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class Suscripcion
    {
        [Key]
        public int Id { get; set; }

        // =========================
        // Relaciones
        // =========================
        [Required]
        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        [Required]
        public int PlanId { get; set; }
        public Plan Plan { get; set; } = null!;

        // =========================
        // Stripe (FUENTE DE VERDAD)
        // =========================
        [Required]
        public string StripeCustomerId { get; set; } = null!;

        [Required]
        public string StripeSubscriptionId { get; set; } = null!;

        [Required]
        public string StripePriceId { get; set; } = null!;

        public string? StripeCheckoutSessionId { get; set; }

        // =========================
        // Estado Stripe
        // active | past_due | unpaid | canceled | incomplete
        // =========================
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "active";

        // =========================
        // Periodos (Stripe)
        // 👉 se llenan SOLO desde invoice.payment_succeeded
        // =========================
        public DateTime? CurrentPeriodStart { get; set; }
        public DateTime? CurrentPeriodEnd { get; set; }

        public DateTime? CanceledAt { get; set; }

        // =========================
        // Control
        // =========================
        public bool AutoRenew { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
