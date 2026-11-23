using AdLocalAPI.Models;
using static AdLocalAPI.DTOs.PagosComercio;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IConfiguracionPagoComercioService
    {
        Task<ApiResponse<ConfiguracionPagoComercioResponseDto?>>
            Obtener(long comercioId = 0);

        Task<ApiResponse<ConfiguracionPagoComercioResponseDto>>
            Guardar(
                ConfiguracionPagoComercioDto dto,
                long comercioId = 0
            );
    }
}