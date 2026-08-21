using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Services.Interfaces
{
    public interface INotificacionService
    {
        Task<ApiResponse<ResumenNotificacionesDto>> ObtenerAsync(int limite = 20);
        Task<ApiResponse<object>> MarcarLeidaAsync(Guid uuid);
        Task<ApiResponse<object>> MarcarTodasLeidasAsync();
        Task NotificarComercioAsync(Pedido pedido, TipoNotificacionPedido tipo, string titulo, string mensaje);
        Task NotificarClienteAsync(Pedido pedido, TipoNotificacionPedido tipo, string titulo, string mensaje);
    }
}
