using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class Usuario
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Rol { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public bool EmailVerificado { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        public DateTime? UltimoAcceso { get; set; }

        public long? ComercioId { get; set; }

        [MaxLength(500)]
        public string? FotoUrl { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(100)]
        public string? StripeCustomerId { get; set; }

        public string? Token { get; set; }

        [MaxLength(100)]
        public string? Codigo { get; set; }

        [MaxLength(50)]
        public string? CodigoReferido { get; set; }

        public bool RedeemMonthFree { get; set; } = false;

        public bool RedeemRewards { get; set; } = false;

        public ICollection<Comercio> Comercios { get; set; } = new List<Comercio>();

        public ICollection<Suscripcion> Suscripciones { get; set; } = new List<Suscripcion>();
        public ICollection<DireccionUsuario> Direcciones { get; set; } = new List<DireccionUsuario>();
    }
    public static class RolesUsuario
    {
        public const string Administrador = "Administrador";
        public const string Comercio = "Comercio";
        public const string Colaborador = "Colaborador";
        public const string Cliente = "Cliente";
    }
}
