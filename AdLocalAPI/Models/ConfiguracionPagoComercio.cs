using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("configuracion_pago_comercio")]
    public class ConfiguracionPagoComercio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        public long IdComercio { get; set; }

        [Required]
        public bool AceptaEfectivo { get; set; } = true;

        [Required]
        public bool AceptaTransferencia { get; set; } = false;

        [MaxLength(300)]
        public string? InstruccionesTransferencia { get; set; }

        [Required]
        public bool Activo { get; set; } = true;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        [ForeignKey(nameof(IdComercio))]
        public Comercio Comercio { get; set; } = null!;
    }
}