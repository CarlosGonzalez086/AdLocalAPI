using AdLocalAPI.Data;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class CuentaBancariaComercioRepository
        : ICuentaBancariaComercioRepository
    {
        private readonly AppDbContext _context;

        public CuentaBancariaComercioRepository(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CuentaBancariaComercio>>
            ObtenerTodasAsync(long idComercio)
        {
            return await _context.CuentasBancariasComercio
                .Where(x =>
                    x.IdComercio == idComercio
                )
                .OrderByDescending(x =>
                    x.Principal
                )
                .ThenByDescending(x =>
                    x.FechaCreacion
                )
                .ToListAsync();
        }

        public async Task<CuentaBancariaComercio?>
            ObtenerPorUuidAsync(
                long idComercio,
                Guid uuid)
        {
            return await _context.CuentasBancariasComercio
                .FirstOrDefaultAsync(x =>
                    x.IdComercio == idComercio &&
                    x.Uuid == uuid
                );
        }

        public async Task<CuentaBancariaComercio?>
            ObtenerPrincipalAsync(
                long idComercio)
        {
            return await _context.CuentasBancariasComercio
                .FirstOrDefaultAsync(x =>
                    x.IdComercio == idComercio &&
                    x.Activo &&
                    x.Principal
                );
        }

        public async Task<CuentaBancariaComercio>
            CrearAsync(
                CuentaBancariaComercio cuenta)
        {
            await _context.CuentasBancariasComercio
                .AddAsync(cuenta);

            await _context.SaveChangesAsync();

            return cuenta;
        }

        public async Task ActualizarAsync(
            CuentaBancariaComercio cuenta)
        {
            _context.CuentasBancariasComercio
                .Update(cuenta);

            await _context.SaveChangesAsync();
        }

        public async Task QuitarPrincipalAsync(
            long idComercio,
            long? exceptoId = null)
        {
            var query =
                _context.CuentasBancariasComercio
                    .Where(x =>
                        x.IdComercio == idComercio &&
                        x.Principal
                    );

            if (exceptoId.HasValue)
            {
                query =
                    query.Where(x =>
                        x.Id != exceptoId.Value
                    );
            }

            var cuentas =
                await query.ToListAsync();

            foreach (var cuenta in cuentas)
            {
                cuenta.Principal =
                    false;

                cuenta.FechaActualizacion =
                    DateTime.UtcNow;
            }

            if (cuentas.Count > 0)
            {
                _context.CuentasBancariasComercio
                    .UpdateRange(cuentas);

                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool>
            TieneCuentaActivaAsync(
                long idComercio)
        {
            return await _context.CuentasBancariasComercio
                .AnyAsync(x =>
                    x.IdComercio == idComercio &&
                    x.Activo
                );
        }

        public async Task<CuentaBancariaComercio?>
            ObtenerPrimeraActivaAsync(
                long idComercio,
                long? exceptoId = null)
        {
            var query =
                _context.CuentasBancariasComercio
                    .Where(x =>
                        x.IdComercio == idComercio &&
                        x.Activo
                    );

            if (exceptoId.HasValue)
            {
                query =
                    query.Where(x =>
                        x.Id != exceptoId.Value
                    );
            }

            return await query
                .OrderByDescending(x =>
                    x.FechaCreacion
                )
                .FirstOrDefaultAsync();
        }
    }
}