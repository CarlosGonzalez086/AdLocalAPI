using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class ComercioRepository
    {
        private readonly AppDbContext _context;

        public ComercioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Comercio>> GetAllAsync()
        {
            return await _context.Comercios.ToListAsync();
        }

        public async Task<Comercio> GetByIdAsync(int id)
        {
            return await _context.Comercios.FindAsync(id);
        }

        public async Task<Comercio> GetComercioByUser(long idUSer)
        {
            return await _context.Comercios
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdUsuario == idUSer && c.Activo);
        }

        public async Task<Comercio> CreateAsync(Comercio comercio)
        {
            _context.Comercios.Add(comercio);
            await _context.SaveChangesAsync();
            return comercio;
        }

        public async Task UpdateAsync(Comercio comercio)
        {
            _context.Comercios.Update(comercio);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var comercio = await _context.Comercios.FindAsync(id);
            if (comercio != null)
            {
                _context.Comercios.Remove(comercio);
                await _context.SaveChangesAsync();
            }
        }
    }
}
