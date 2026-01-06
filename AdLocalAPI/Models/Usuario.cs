using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Rol { get; set; } // Admin o Cliente

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Relación opcional con Comercio
        public int? ComercioId { get; set; }
        public string? FotoUrl { get; set; }
        public string? StripeCustomerId { get; set; }
        public Comercio? Comercio { get; set; }
    }
}
