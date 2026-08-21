using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.UsuarioCliente;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Services
{
    public class PedidoClienteService : IPedidoClienteService
    {
        private readonly IPedidoRepository _repository;
        private readonly JwtContext _jwtContext;

        public PedidoClienteService(
            IPedidoRepository repository,
            JwtContext jwtContext)
        {
            _repository = repository;
            _jwtContext = jwtContext;
        }

        public async Task<ApiResponse<PagedResponse<PedidoClienteListadoDto>>>
            ObtenerTodosAsync(
                int page,
                int pageSize,
                EstadoPagoPedido? estadoPago)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var resultado = await _repository.ObtenerPedidosClienteAsync(
                _jwtContext.GetUserId(), page, pageSize, estadoPago);

            return ApiResponse<PagedResponse<PedidoClienteListadoDto>>.Success(
                resultado,
                "Pedidos obtenidos correctamente."
            );
        }

        public async Task<ApiResponse<PedidoClienteDetalleDto>>
            ObtenerDetalleAsync(Guid pedidoUuid)
        {
            var pedido = await _repository.ObtenerDetallePedidoClienteAsync(
                pedidoUuid,
                _jwtContext.GetUserId()
            );

            return pedido == null
                ? ApiResponse<PedidoClienteDetalleDto>.Error(
                    "404", "No se encontró el pedido.")
                : ApiResponse<PedidoClienteDetalleDto>.Success(
                    pedido, "Pedido obtenido correctamente.");
        }
    }
}
