using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class Usuario
    {
        public long Id { get; set; }

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
        public long? ComercioId { get; set; }
        public string? FotoUrl { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? Token { get; set; }
        public string? Codigo { get; set; }
        public ICollection<Comercio> Comercios { get; set; } = new List<Comercio>();
    }
}
