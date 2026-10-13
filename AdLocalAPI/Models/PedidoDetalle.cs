using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("pedido_detalles")]
    public class PedidoDetalle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        public long IdPedido { get; set; }

        /*
         * Nullable intencionalmente.
         *
         * El pedido histórico debe seguir existiendo
         * aunque el producto posteriormente sea eliminado.
         */
        public long? IdProductoServicio { get; set; }

        [Required]
        public Guid ProductoUuid { get; set; }

        // ==========================================
        // SNAPSHOT DEL PRODUCTO
        // ==========================================

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        public string? LogoUrl { get; set; }

        [MaxLength(100)]
        public string? CodigoInterno { get; set; }

        [Required]
        public int Cantidad { get; set; }

        // Null means a legacy checkout without a reliable inventory snapshot.
        public int? StockDescontado { get; set; }
        public int StockPorRestituir { get; set; }
        public int StockRestituido { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal PrecioUnitario { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Subtotal { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; }
            = DateTime.UtcNow;

        public Pedido Pedido { get; set; } = null!;
    }
}
