using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("carrito_detalles")]
    public class CarritoDetalle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        public long IdCarrito { get; set; }

        [Required]
        public long IdProductoServicio { get; set; }

        [Required]
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "La cantidad debe ser mayor a 0."
        )]
        public int Cantidad { get; set; } = 1;

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal PrecioUnitario { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Subtotal { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        [Required]
        public bool Activo { get; set; } = true;

        [ForeignKey(nameof(IdCarrito))]
        public Carrito Carrito { get; set; } = null!;

        [ForeignKey(nameof(IdProductoServicio))]
        public ProductosServicios ProductoServicio { get; set; } = null!;
    }
}