using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("comisiones")]
    public class Comision
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        public long IdComercio { get; set; }

        [Required]
        public int TipoOperacion { get; set; }

        [Required]
        public long IdReferencia { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal MontoOperacion { get; set; }

        [Required]
        [Column(TypeName = "numeric(5,2)")]
        public decimal PorcentajeComision { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal MontoComision { get; set; }

        [Required]
        public int Estatus { get; set; } = 1;

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaPago { get; set; }

        public DateTime? FechaCancelacion { get; set; }

        [Required]
        public bool Activo { get; set; } = true;
    }
}