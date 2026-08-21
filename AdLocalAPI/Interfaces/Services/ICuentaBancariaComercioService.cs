using AdLocalAPI.Models;
using static AdLocalAPI.DTOs.PagosComercio;

namespace AdLocalAPI.Services.Interfaces
{
    public interface ICuentaBancariaComercioService
    {
        Task<ApiResponse<
            IEnumerable<CuentaBancariaComercioResponseDto>
        >> ObtenerTodas();

        Task<ApiResponse<
            CuentaBancariaComercioResponseDto
        >> Crear(
            CuentaBancariaComercioCreateDto dto
        );

        Task<ApiResponse<bool>>
            Actualizar(
                Guid uuid,
                CuentaBancariaComercioUpdateDto dto
            );

        Task<ApiResponse<bool>>
            Eliminar(Guid uuid);

        Task<ApiResponse<bool>>
            EstablecerPrincipal(Guid uuid);
    }
}