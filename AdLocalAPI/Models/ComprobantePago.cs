using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("comprobantes_pago")]
    public class ComprobantePago
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        public long IdPedido { get; set; }

        [Required]
        public long IdUsuario { get; set; }

        [Required]
        [MaxLength(500)]
        public string ArchivoUrl { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        public int Estatus { get; set; } = 1;

        public long? IdUsuarioValidacion { get; set; }

        [MaxLength(500)]
        public string? Comentario { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaValidacion { get; set; }

        public bool Activo { get; set; } = true;
    }
}