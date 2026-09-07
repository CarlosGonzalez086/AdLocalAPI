using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.Data;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class CuentaBancariaAdLocalRepository : ICuentaBancariaAdLocalRepository
    {
        private readonly AppDbContext _context;

        public CuentaBancariaAdLocalRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CuentaBancariaAdLocal>> ObtenerTodasAsync()
        {
            return await _context.CuentasBancariasAdLocal
                .AsNoTracking()
                .OrderByDescending(x => x.Principal)
                .ThenBy(x => x.Banco)
                .ToListAsync();
        }

        public async Task<CuentaBancariaAdLocal?> ObtenerPrincipalAsync()
        {
            return await _context.CuentasBancariasAdLocal
                .AsNoTracking()
                .Where(x => x.Activo)
                .OrderByDescending(x => x.Principal)
                .ThenByDescending(x => x.FechaCreacion)
                .FirstOrDefaultAsync();
        }

        public async Task<CuentaBancariaAdLocal?> ObtenerPorUuidAsync(Guid uuid)
        {
            return await _context.CuentasBancariasAdLocal
                .FirstOrDefaultAsync(x => x.Uuid == uuid);
        }

        public async Task<CuentaBancariaAdLocal?> ObtenerPorIdAsync(long id)
        {
            return await _context.CuentasBancariasAdLocal
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<CuentaBancariaAdLocal> CrearAsync(CuentaBancariaAdLocal cuenta)
        {
            _context.CuentasBancariasAdLocal.Add(cuenta);
            await _context.SaveChangesAsync();
            return cuenta;
        }

        public async Task ActualizarAsync(CuentaBancariaAdLocal cuenta)
        {
            _context.CuentasBancariasAdLocal.Update(cuenta);
            await _context.SaveChangesAsync();
        }

        public async Task QuitarPrincipalAsync(long exceptoId = 0)
        {
            var actuales = await _context.CuentasBancariasAdLocal
                .Where(x => x.Principal && x.Id != exceptoId)
                .ToListAsync();

            foreach (var x in actuales)
            {
                x.Principal = false;
            }

            await _context.SaveChangesAsync();
        }
    }
}
