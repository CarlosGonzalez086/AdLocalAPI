using AdLocalAPI.Data;
using AdLocalAPI.Interfaces.Repository;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class ConfiguracionPagoComercioRepository
        : IConfiguracionPagoComercioRepository
    {
        private readonly AppDbContext _context;

        public ConfiguracionPagoComercioRepository(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<ConfiguracionPagoComercio?>
            ObtenerPorComercioAsync(long idComercio)
        {
            return await _context.ConfiguracionPagoComercios
                .FirstOrDefaultAsync(x =>
                    x.IdComercio == idComercio
                );
        }

        public async Task<ConfiguracionPagoComercio> CrearAsync(
            ConfiguracionPagoComercio configuracion)
        {
            await _context.ConfiguracionPagoComercios
                .AddAsync(configuracion);

            await _context.SaveChangesAsync();

            return configuracion;
        }

        public async Task ActualizarAsync(
            ConfiguracionPagoComercio configuracion)
        {
            _context.ConfiguracionPagoComercios
                .Update(configuracion);

            await _context.SaveChangesAsync();
        }
    }
}