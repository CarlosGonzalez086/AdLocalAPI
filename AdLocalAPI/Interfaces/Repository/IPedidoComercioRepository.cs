using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IPedidoComercioRepository
    {
        Task<List<ComercioPedidoSelectorDto>> ObtenerComerciosAsync(long idUsuario, string rol);
        Task<bool> PuedeGestionarAsync(long idUsuario, string rol, long idComercio);
        Task<PedidosComercioDashboardDto> ObtenerDashboardAsync(long idComercio);
        Task<PagedResponse<PedidoComercioListadoDto>> ObtenerPedidosAsync(
            long idComercio, int page, int pageSize, EstadoPedido? estado);
        Task<PedidoComercioDetalleDto?> ObtenerDetalleAsync(long idComercio, Guid pedidoUuid);
        Task<Pedido?> ObtenerPedidoTrackingAsync(long idComercio, Guid pedidoUuid);
        Task<ComprobantePago?> ObtenerComprobanteTrackingAsync(long idPedido);
        Task GuardarEstadoAsync(Pedido pedido, PedidoHistorialEstado historial);
        Task GuardarPagoAsync(Pedido pedido, ComprobantePago? comprobante);
    }
}
