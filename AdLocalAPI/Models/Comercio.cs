using System.ComponentModel.DataAnnotations;

namespace AdLocalAPI.Models
{
    public class Comercio
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; }
        // 🔐 Relación con usuario
        [Required]
        public long IdUsuario { get; set; }

        public string? Descripcion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? LogoUrl { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
