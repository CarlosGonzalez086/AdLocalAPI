using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("pagos_comisiones")]
    public class PagoComision
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public long Id { get; set; }
        [Required] public Guid Uuid { get; set; } = Guid.NewGuid();
        [Required] public long IdComercio { get; set; }
        [Required] public long IdCuentaBancariaAdLocal { get; set; }
        [Required, MaxLength(20)] public string Periodo { get; set; } = "semana";
        [Required, MaxLength(20)] public string MetodoPago { get; set; } = "transferencia";
        [Required, Column(TypeName = "numeric(18,2)")] public decimal Monto { get; set; }
        [Required, MaxLength(1000)] public string ComprobanteUrl { get; set; } = string.Empty;
        [Required] public int Estatus { get; set; } = 1;
        [MaxLength(500)] public string? Comentario { get; set; }
        public long IdUsuarioCreacion { get; set; }
        public long? IdUsuarioRevision { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaRevision { get; set; }
        public bool Activo { get; set; } = true;
        public ICollection<PagoComisionDetalle> Detalles { get; set; } = new List<PagoComisionDetalle>();
    }

    [Table("pagos_comisiones_detalle")]
    public class PagoComisionDetalle
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public long Id { get; set; }
        [Required] public long IdPagoComision { get; set; }
        [Required] public long IdComision { get; set; }
        [Required, Column(TypeName = "numeric(18,2)")] public decimal Monto { get; set; }
        public PagoComision PagoComision { get; set; } = null!;
    }
}
