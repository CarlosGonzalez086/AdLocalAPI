using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("calificaciones_comentarios")]
    public class CalificacionComentario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
        public int Calificacion { get; set; }

        [Required]
        [MaxLength(250, ErrorMessage = "El comentario no puede exceder los 250 caracteres.")]
        public string Comentario { get; set; }

        [Required]
        public long IdComercio { get; set; }

        [Required]
        [MaxLength(100)]
        public string NombrePersona { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        // 🔹 Navegación
        [ForeignKey(nameof(IdComercio))]
        public Comercio Comercio { get; set; }
    }
}
