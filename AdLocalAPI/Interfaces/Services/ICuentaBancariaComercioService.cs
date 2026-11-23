using AdLocalAPI.Models;
using static AdLocalAPI.DTOs.PagosComercio;

namespace AdLocalAPI.Services.Interfaces
{
    public interface ICuentaBancariaComercioService
    {
        Task<ApiResponse<
            IEnumerable<CuentaBancariaComercioResponseDto>
        >> ObtenerTodas(long comercioId = 0);

        Task<ApiResponse<
            CuentaBancariaComercioResponseDto
        >> Crear(
            CuentaBancariaComercioCreateDto dto,
            long comercioId = 0
        );

        Task<ApiResponse<bool>>
            Actualizar(
                Guid uuid,
                CuentaBancariaComercioUpdateDto dto,
                long comercioId = 0
            );

        Task<ApiResponse<bool>>
            Eliminar(Guid uuid, long comercioId = 0);

        Task<ApiResponse<bool>>
            EstablecerPrincipal(Guid uuid, long comercioId = 0);
    }
}