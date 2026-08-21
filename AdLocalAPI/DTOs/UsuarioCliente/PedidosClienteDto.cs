using AdLocalAPI.Utils;

namespace AdLocalAPI.DTOs.UsuarioCliente
{
    public class PedidoClienteListadoDto
    {
        public Guid Uuid { get; set; }
        public string NumeroPedido { get; set; } = string.Empty;
        public string Comercio { get; set; } = string.Empty;
        public string? ComercioLogoUrl { get; set; }
        public decimal Total { get; set; }
        public EstadoPedido Estado { get; set; }
        public EstadoPagoPedido EstadoPago { get; set; }
        public MetodoPagoPedido MetodoPago { get; set; }
        public TipoEntregaPedido TipoEntrega { get; set; }
        public int TotalProductos { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaComprobantePago { get; set; }
        public bool PuedeSubirComprobante { get; set; }
    }

    public class PedidoClienteDetalleDto : PedidoClienteListadoDto
    {
        public string? ObservacionesCliente { get; set; }
        public string? Direccion { get; set; }
        public string? TelefonoEntrega { get; set; }
        public string? Banco { get; set; }
        public string? Beneficiario { get; set; }
        public string? NumeroCuenta { get; set; }
        public string? Clabe { get; set; }
        public string? NumeroTarjeta { get; set; }
        public string? InstruccionesTransferencia { get; set; }
        public List<PedidoClienteProductoDto> Productos { get; set; } = new();
        public List<PedidoClienteHistorialDto> Historial { get; set; } = new();
    }

    public class PedidoClienteProductoDto
    {
        public Guid Uuid { get; set; }
        public Guid ProductoUuid { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string? Observaciones { get; set; }
    }

    public class PedidoClienteHistorialDto
    {
        public EstadoPedido Estado { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha { get; set; }
    }
}
