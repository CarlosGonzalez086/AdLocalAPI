using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Repository
{
    public interface IConfiguracionPagoComercioRepository
    {
        Task<ConfiguracionPagoComercio?> ObtenerPorComercioAsync(
            long idComercio
        );

        Task<ConfiguracionPagoComercio> CrearAsync(
            ConfiguracionPagoComercio configuracion
        );

        Task ActualizarAsync(
            ConfiguracionPagoComercio configuracion
        );
    }
}