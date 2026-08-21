using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("pedido_historial")]
    public class PedidoHistorial
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        public long IdPedido { get; set; }

        public int? EstatusAnterior { get; set; }

        [Required]
        public int EstatusNuevo { get; set; }

        public long? IdUsuario { get; set; }

        [MaxLength(500)]
        public string? Comentario { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}