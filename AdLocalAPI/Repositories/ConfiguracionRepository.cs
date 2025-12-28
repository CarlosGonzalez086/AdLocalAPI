using AdLocalAPI.Data;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class ConfiguracionRepository : IConfiguracionRepository
    {
        private readonly AppDbContext _context;

        public ConfiguracionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ConfiguracionSistema?> ObtenerPorKeyAsync(string key)
        {
            return await _context.ConfiguracionSistema.FirstOrDefaultAsync(s => s.Key == key);
        }

        public async Task<ConfiguracionSistema> InsertarAsync(ConfiguracionSistema config)
        {
            _context.ConfiguracionSistema.Add(config);
            await _context.SaveChangesAsync();
            return config;
        }

        public async Task<ConfiguracionSistema> ActualizarAsync(ConfiguracionSistema config)
        {
            _context.ConfiguracionSistema.Update(config);
            await _context.SaveChangesAsync();
            return config;
        }
        public async Task<List<ConfiguracionSistema>> ObtenerTodosAsync()
        {
            return await _context.ConfiguracionSistema
                .OrderBy(x => x.Key)
                .ToListAsync();
        }
    }
}
