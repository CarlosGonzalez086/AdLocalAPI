using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IPedidoComercioService
    {
        Task<ApiResponse<List<ComercioPedidoSelectorDto>>> ObtenerComerciosAsync();
        Task<ApiResponse<PedidosComercioDashboardDto>> ObtenerDashboardAsync(long comercioId);
        Task<ApiResponse<PagedResponse<PedidoComercioListadoDto>>> ObtenerPedidosAsync(
            long comercioId, int page, int pageSize, EstadoPedido? estado);
        Task<ApiResponse<PedidoComercioDetalleDto>> ObtenerDetalleAsync(long comercioId, Guid pedidoUuid);
        Task<ApiResponse<PedidoComercioDetalleDto>> CambiarEstadoAsync(
            long comercioId, Guid pedidoUuid, CambiarEstadoPedidoDto dto);
        Task<ApiResponse<PedidoComercioDetalleDto>> RevisarPagoAsync(
            long comercioId, Guid pedidoUuid, RevisarPagoPedidoDto dto);
        Task<ApiResponse<ArchivoComprobanteDto>> ObtenerComprobanteAsync(long comercioId, Guid pedidoUuid);
    }
}
