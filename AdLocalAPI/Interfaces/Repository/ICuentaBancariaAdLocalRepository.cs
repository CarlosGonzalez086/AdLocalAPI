using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface ICuentaBancariaAdLocalRepository
    {
        Task<List<CuentaBancariaAdLocal>> ObtenerTodasAsync();
        Task<CuentaBancariaAdLocal?> ObtenerPrincipalAsync();
        Task<CuentaBancariaAdLocal?> ObtenerPorUuidAsync(Guid uuid);
        Task<CuentaBancariaAdLocal?> ObtenerPorIdAsync(long id);
        Task<CuentaBancariaAdLocal> CrearAsync(CuentaBancariaAdLocal cuenta);
        Task ActualizarAsync(CuentaBancariaAdLocal cuenta);
        Task QuitarPrincipalAsync(long exceptoId = 0);
    }
}
