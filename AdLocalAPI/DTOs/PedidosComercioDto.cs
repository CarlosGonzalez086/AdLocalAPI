using AdLocalAPI.Utils;

namespace AdLocalAPI.DTOs
{
    public class ComercioPedidoSelectorDto
    {
        public long Id { get; set; }
        public Guid Uuid { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class PedidosComercioDashboardDto
    {
        public decimal VentasHoy { get; set; }
        public decimal VentasSemana { get; set; }
        public int PedidosHoy { get; set; }
        public int PendientesAprobacion { get; set; }
        public int ComprobantesPendientes { get; set; }
        public List<VentaDiaComercioDto> VentasPorDia { get; set; } = new();
    }

    public class VentaDiaComercioDto
    {
        public DateTime Fecha { get; set; }
        public string Dia { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public int Pedidos { get; set; }
    }

    public class PedidoComercioListadoDto
    {
        public Guid Uuid { get; set; }
        public string NumeroPedido { get; set; } = string.Empty;
        public string ClienteNombre { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public EstadoPedido Estado { get; set; }
        public EstadoPagoPedido EstadoPago { get; set; }
        public MetodoPagoPedido MetodoPago { get; set; }
        public TipoEntregaPedido TipoEntrega { get; set; }
        public int TotalProductos { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool TieneComprobante { get; set; }
        public List<EstadoPedido> AccionesDisponibles { get; set; } = new();
    }

    public class PedidoComercioDetalleDto : PedidoComercioListadoDto
    {
        public string? ClienteEmail { get; set; }
        public string? TelefonoEntrega { get; set; }
        public string? Direccion { get; set; }
        public string? ObservacionesCliente { get; set; }
        public DateTime? FechaComprobantePago { get; set; }
        public List<PedidoComercioProductoDto> Productos { get; set; } = new();
        public List<PedidoComercioHistorialDto> Historial { get; set; } = new();
    }

    public class PedidoComercioProductoDto
    {
        public Guid Uuid { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string? Observaciones { get; set; }
    }

    public class PedidoComercioHistorialDto
    {
        public EstadoPedido Estado { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class CambiarEstadoPedidoDto
    {
        public EstadoPedido Estado { get; set; }
        public string? Comentario { get; set; }
    }

    public class RevisarPagoPedidoDto
    {
        public EstadoPagoPedido EstadoPago { get; set; }
        public string? Comentario { get; set; }
    }

    public class ArchivoComprobanteDto
    {
        public byte[] Contenido { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
        public string Nombre { get; set; } = "comprobante";
    }
}
