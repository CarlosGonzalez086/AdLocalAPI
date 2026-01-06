using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Tarjetas
{
    public interface ITarjetaService
    {
        Task<ApiResponse<object>> CrearTarjeta(
            CrearTarjetaDto dto
        );

        Task<ApiResponse<object>> SetDefault(
            long tarjetaId
        );

        Task<ApiResponse<object>> EliminarTarjeta(
            long tarjetaId
        );
        Task<ApiResponse<List<TarjetaDto>>> Listar();
    }

}
