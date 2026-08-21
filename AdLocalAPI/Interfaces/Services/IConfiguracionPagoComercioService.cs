using AdLocalAPI.Models;
using static AdLocalAPI.DTOs.PagosComercio;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IConfiguracionPagoComercioService
    {
        Task<ApiResponse<ConfiguracionPagoComercioResponseDto?>>
            Obtener();

        Task<ApiResponse<ConfiguracionPagoComercioResponseDto>>
            Guardar(
                ConfiguracionPagoComercioDto dto
            );
    }
}