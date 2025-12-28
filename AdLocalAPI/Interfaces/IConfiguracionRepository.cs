using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces
{
    public interface IConfiguracionRepository
    {
        Task<ConfiguracionSistema?> ObtenerPorKeyAsync(string key);
        Task<ConfiguracionSistema> InsertarAsync(ConfiguracionSistema config);
        Task<ConfiguracionSistema> ActualizarAsync(ConfiguracionSistema config);
        Task<List<ConfiguracionSistema>> ObtenerTodosAsync();
    }

}
