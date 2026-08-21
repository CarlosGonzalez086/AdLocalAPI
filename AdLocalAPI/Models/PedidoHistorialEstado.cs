using AdLocalAPI.Utils;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("pedido_historial_estados")]
    public class PedidoHistorialEstado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        public long IdPedido { get; set; }

        public EstadoPedido? EstadoAnterior { get; set; }

        [Required]
        public EstadoPedido EstadoNuevo { get; set; }

        /*
         * Usuario que ejecutó el cambio.
         *
         * Puede ser cliente, comercio, colaborador
         * o administrador.
         */
        public long? IdUsuarioCambio { get; set; }

        [MaxLength(500)]
        public string? Comentario { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; }
            = DateTime.UtcNow;

        public Pedido Pedido { get; set; } = null!;
    }
}