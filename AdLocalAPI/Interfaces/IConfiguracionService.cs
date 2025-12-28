using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces
{
    public interface IConfiguracionService
    {
        Task<ApiResponse<ConfiguracionSistema>> CrearOActualizarAsync(ConfiguracionSistemaDto dto);
        Task<ApiResponse<List<ConfiguracionSistema>>> ObtenerTodosAsync();
        Task<ApiResponse<List<ConfiguracionSistema>>> RegistrarStripeAsync(StripeConfiguracionDto dto);
    }
}



