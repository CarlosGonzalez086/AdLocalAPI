using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.UsuarioCliente;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IPedidoClienteService
    {
        Task<ApiResponse<PagedResponse<PedidoClienteListadoDto>>> ObtenerTodosAsync(
            int page,
            int pageSize,
            EstadoPagoPedido? estadoPago
        );

        Task<ApiResponse<PedidoClienteDetalleDto>> ObtenerDetalleAsync(
            Guid pedidoUuid
        );
    }
}
