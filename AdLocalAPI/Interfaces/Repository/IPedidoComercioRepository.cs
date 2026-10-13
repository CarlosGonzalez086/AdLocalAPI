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
        Task<Pedido> CambiarEstadoAsync(long idComercio, Guid uuid, long actor, CambiarEstadoPedidoDto dto, CancellationToken ct);
        Task<Pedido> RevisarPagoAsync(long idComercio, Guid uuid, long actor, RevisarPagoPedidoDto dto, CancellationToken ct);
        Task<Pedido> ReembolsarAsync(long idComercio, Guid uuid, long actor, RegistrarReembolsoPedidoDto dto, CancellationToken ct);
        Task<Pedido> ConciliarInventarioAsync(long idComercio, Guid uuid, long actor, ConciliarInventarioPedidoDto dto, CancellationToken ct);
    }
}
