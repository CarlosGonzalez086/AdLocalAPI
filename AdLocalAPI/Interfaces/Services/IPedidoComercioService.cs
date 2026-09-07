using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IPedidoComercioService
    {
        Task<ApiResponse<List<ComercioPedidoSelectorDto>>> ObtenerComerciosAsync(CancellationToken cancellationToken = default);
        Task<ApiResponse<PedidosComercioDashboardDto>> ObtenerDashboardAsync(long comercioId, CancellationToken cancellationToken = default);
        Task<ApiResponse<PagedResponse<PedidoComercioListadoDto>>> ObtenerPedidosAsync(
            long comercioId, int page, int pageSize, EstadoPedido? estado, CancellationToken cancellationToken = default);
        Task<ApiResponse<PedidoComercioDetalleDto>> ObtenerDetalleAsync(long comercioId, Guid pedidoUuid, CancellationToken cancellationToken = default);
        Task<ApiResponse<PedidoComercioDetalleDto>> CambiarEstadoAsync(
            long comercioId, Guid pedidoUuid, CambiarEstadoPedidoDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<PedidoComercioDetalleDto>> RevisarPagoAsync(
            long comercioId, Guid pedidoUuid, RevisarPagoPedidoDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<ArchivoComprobanteDto>> ObtenerComprobanteAsync(long comercioId, Guid pedidoUuid, CancellationToken cancellationToken = default);
    }
}
