using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface ICuentaBancariaComercioRepository
    {
        Task<List<CuentaBancariaComercio>>
            ObtenerTodasAsync(
                long idComercio
            );

        Task<CuentaBancariaComercio?>
            ObtenerPorUuidAsync(
                long idComercio,
                Guid uuid
            );

        Task<CuentaBancariaComercio?>
            ObtenerPrincipalAsync(
                long idComercio
            );

        Task<CuentaBancariaComercio>
            CrearAsync(
                CuentaBancariaComercio cuenta
            );

        Task ActualizarAsync(
            CuentaBancariaComercio cuenta
        );

        Task QuitarPrincipalAsync(
            long idComercio,
            long? exceptoId = null
        );

        Task<bool> TieneCuentaActivaAsync(
            long idComercio
        );

        Task<CuentaBancariaComercio?>
            ObtenerPrimeraActivaAsync(
                long idComercio,
                long? exceptoId = null
            );
    }
}