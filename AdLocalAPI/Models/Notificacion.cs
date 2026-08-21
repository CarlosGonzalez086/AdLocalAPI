using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("notificaciones")]
    public class Notificacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        public long IdUsuario { get; set; }

        [Required]
        [MaxLength(150)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        [Required]
        public int TipoNotificacion { get; set; }

        public long? IdReferencia { get; set; }

        [MaxLength(100)]
        public string? TipoReferencia { get; set; }

        [Required]
        public bool Leida { get; set; } = false;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaLectura { get; set; }

        public bool Activo { get; set; } = true;
    }
}