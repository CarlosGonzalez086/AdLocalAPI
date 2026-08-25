using AdLocalAPI.Utils;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdLocalAPI.Models
{
    [Table("pedidos")]
    public class Pedido
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid Uuid { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(40)]
        public string NumeroPedido { get; set; } = string.Empty;

        // ==========================================
        // RELACIONES
        // ==========================================

        [Required]
        public long IdUsuario { get; set; }

        [Required]
        public long IdComercio { get; set; }

        public long? IdDireccionUsuario { get; set; }

        // ==========================================
        // ESTADOS
        // ==========================================

        [Required]
        public EstadoPedido Estado { get; set; }
            = EstadoPedido.PendienteAprobacion;

        [Required]
        public EstadoPagoPedido EstadoPago { get; set; }
            = EstadoPagoPedido.Pendiente;

        [Required]
        public MetodoPagoPedido MetodoPago { get; set; }

        [Required]
        public TipoEntregaPedido TipoEntrega { get; set; }

        // ==========================================
        // IMPORTES
        // ==========================================

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Subtotal { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal CostoEnvio { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Total { get; set; }

        // ==========================================
        // COMISIÓN ADLOCAL
        // ==========================================

        [Required]
        [Column(TypeName = "numeric(8,4)")]
        public decimal PorcentajeComision { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal ComisionFija { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal MontoComision { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal MontoComercio { get; set; }

        // ==========================================
        // SNAPSHOT COMERCIO
        // ==========================================

        [Required]
        [MaxLength(150)]
        public string ComercioNombre { get; set; } = string.Empty;

        public string? ComercioLogoUrl { get; set; }

        // ==========================================
        // SNAPSHOT CLIENTE
        // ==========================================

        [Required]
        [MaxLength(150)]
        public string ClienteNombre { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ClienteEmail { get; set; }

        // ==========================================
        // SNAPSHOT DIRECCIÓN
        // ==========================================

        [MaxLength(50)]
        public string? DireccionAlias { get; set; }

        [MaxLength(200)]
        public string? DireccionCalle { get; set; }

        [MaxLength(20)]
        public string? DireccionNumeroExterior { get; set; }

        [MaxLength(20)]
        public string? DireccionNumeroInterior { get; set; }

        [MaxLength(150)]
        public string? DireccionColonia { get; set; }

        [MaxLength(10)]
        public string? DireccionCodigoPostal { get; set; }

        [MaxLength(150)]
        public string? DireccionEstado { get; set; }

        [MaxLength(150)]
        public string? DireccionMunicipio { get; set; }

        [Column(TypeName = "numeric(10,7)")]
        public decimal? DireccionLatitud { get; set; }

        [Column(TypeName = "numeric(10,7)")]
        public decimal? DireccionLongitud { get; set; }

        [MaxLength(500)]
        public string? DireccionReferencias { get; set; }

        [MaxLength(20)]
        public string? TelefonoEntrega { get; set; }

        // ==========================================
        // SNAPSHOT TRANSFERENCIA
        // ==========================================

        [MaxLength(100)]
        public string? Banco { get; set; }

        [MaxLength(150)]
        public string? Beneficiario { get; set; }

        [MaxLength(25)]
        public string? NumeroCuenta { get; set; }

        [MaxLength(18)]
        public string? Clabe { get; set; }

        [MaxLength(19)]
        public string? NumeroTarjeta { get; set; }

        [MaxLength(300)]
        public string? InstruccionesTransferencia { get; set; }

        public string? ComprobantePagoUrl { get; set; }

        public DateTime? FechaComprobantePago { get; set; }

        // ==========================================
        // OBSERVACIONES
        // ==========================================

        [MaxLength(500)]
        public string? ObservacionesCliente { get; set; }

        // ==========================================
        // FECHAS
        // ==========================================

        [Required]
        public DateTime FechaCreacion { get; set; }
            = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        public DateTime? FechaAprobacion { get; set; }

        public DateTime? FechaEntrega { get; set; }

        public DateTime? FechaFinalizacion { get; set; }

        // ==========================================
        // NAVEGACIÓN
        // ==========================================

        public ICollection<PedidoDetalle> Detalles { get; set; }
            = new List<PedidoDetalle>();

        public ICollection<PedidoHistorialEstado> HistorialEstados { get; set; }
            = new List<PedidoHistorialEstado>();
    }
}
