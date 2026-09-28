using System.Threading;
using System.Threading.Tasks;
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
            EstadoPagoPedido? estadoPago,
            CancellationToken cancellationToken = default
        );

        Task<ApiResponse<PedidoClienteDetalleDto>> ObtenerDetalleAsync(
            Guid pedidoUuid,
            CancellationToken cancellationToken = default
        );
    }
}
