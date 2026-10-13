using AdLocalAPI.Utils;

namespace AdLocalAPI.DTOs
{
    public class ComercioPedidoSelectorDto
    {
        public string ZonaHoraria { get; set; } = Helpers.CitaOperacion.ZonaHoraria;
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
        public long Version { get; set; }
        public bool InventarioPendiente { get; set; }
        public bool? RestituirReserva { get; set; }
        public bool PuedeRegistrarPago { get; set; }
        public bool PuedeReembolsar { get; set; }
        public PedidoReembolsoDto? Reembolso { get; set; }
        public decimal Subtotal { get; set; }
        public decimal CostoEnvio { get; set; }
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
        public int? StockDescontado { get; set; }
        public int StockPorRestituir { get; set; }
        public int StockRestituido { get; set; }
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
        public string Evento { get; set; } = "Estado";
        public EstadoPagoPedido? EstadoPago { get; set; }
        public long? IdUsuarioCambio { get; set; }
        public EstadoPedido Estado { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class CambiarEstadoPedidoDto
    {
        [System.Text.Json.Serialization.JsonRequired]
        public long VersionPedido { get; set; }
        public EstadoPedido Estado { get; set; }
        public string? Comentario { get; set; }
        public List<DevolucionPedidoProductoDto>? ProductosRestituibles { get; set; }
    }

    public class RevisarPagoPedidoDto
    {
        [System.Text.Json.Serialization.JsonRequired]
        public long VersionPedido { get; set; }
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
