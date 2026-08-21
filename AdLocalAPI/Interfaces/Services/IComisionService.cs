using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IComisionService
    {
        Task RegistrarVentaAsync(Pedido pedido);
        Task<ApiResponse<ComisionesDashboardDto>> ObtenerDashboardAsync();
        Task<ApiResponse<List<ComisionComercioResumenDto>>> ObtenerResumenAsync(string periodo);
        Task<ApiResponse<PagedResponse<ComisionMovimientoDto>>> ObtenerMovimientosAsync(int page, int pageSize, long? comercioId, int? estatus);
        Task<ApiResponse<object>> LiquidarAsync(long comercioId, string periodo);
    }
}
