using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class Suscripcion
    {
        public int Id { get; set; }

        // Relaciones
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        public int PlanId { get; set; }
        public Plan Plan { get; set; }

        // Stripe
        public string StripeCustomerId { get; set; }
        public string StripeSubscriptionId { get; set; }
        public string StripePriceId { get; set; }

        // Pago
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = "MXN";
        public string MetodoPago { get; set; } = "stripe";

        // Fechas
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime? FechaCancelacion { get; set; }

        // Estado
        public bool Activa { get; set; }
        public string Estado { get; set; } // active, canceled, past_due

        // Control
        public bool AutoRenovacion { get; set; } = true;
        public bool Eliminada { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public string StripeSessionId { get; set; }
    }
}
