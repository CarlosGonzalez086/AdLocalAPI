using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("configuracion_comercio_pedidos")]
    public class ConfiguracionComercioPedido
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        public long IdComercio { get; set; }

        [Required]
        public bool AceptaPedidos { get; set; } = false;

        [Required]
        public bool AceptandoPedidosAhora { get; set; } = false;

        [Required]
        public bool PermiteEfectivo { get; set; } = true;

        [Required]
        public bool PermiteTransferencia { get; set; } = false;

        [Required]
        public bool PermiteRecoger { get; set; } = true;

        [Required]
        public bool PermiteDomicilio { get; set; } = false;

        [Column(TypeName = "numeric(18,2)")]
        public decimal PedidoMinimo { get; set; } = 0;

        [Range(0, 1440, ErrorMessage = "El tiempo de preparación no es válido.")]
        public int TiempoPreparacionMinutos { get; set; } = 0;

        [Column(TypeName = "numeric(18,2)")]
        public decimal? CostoEnvio { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal? CompraMinimaEnvioGratis { get; set; }

        [MaxLength(500)]
        public string? MensajePedidos { get; set; }

        [Required]
        public bool Activo { get; set; } = true;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }
    }
}