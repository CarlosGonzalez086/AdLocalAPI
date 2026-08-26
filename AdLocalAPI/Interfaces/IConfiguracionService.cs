using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Interfaces
{
    public interface IConfiguracionService
    {
        Task<ApiResponse<ConfiguracionSistema>> CrearOActualizarAsync(ConfiguracionSistemaDto dto);
        Task<ApiResponse<List<ConfiguracionSistema>>> ObtenerTodosAsync();
        Task<ApiResponse<List<ConfiguracionSistema>>> RegistrarStripeAsync(StripeConfiguracionDto dto);
        Task<ApiResponse<List<ConfiguracionSistema>>> RegistrarCrearClavesAsync(ClavesConfigDto dto);
        Task<ApiResponse<List<ConfiguracionSistema>>>RegistrarComisionMarketplaceAsync(ComisionMarketplaceDto dto);
        Task<ApiResponse<List<ConfiguracionSistema>>>RegistrarEmailAsync(EmailConfiguracionDto dto);
    }
}



