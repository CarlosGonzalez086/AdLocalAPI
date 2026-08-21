using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("configuracion_comisiones")]
    public class ConfiguracionComision
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        public int TipoOperacion { get; set; }

        [Required]
        [Column(TypeName = "numeric(5,2)")]
        [Range(0, 100, ErrorMessage = "El porcentaje de comisión debe estar entre 0 y 100.")]
        public decimal PorcentajeComision { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal? ComisionMinima { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal? ComisionMaxima { get; set; }

        [Required]
        public bool Activo { get; set; } = true;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }
    }
}