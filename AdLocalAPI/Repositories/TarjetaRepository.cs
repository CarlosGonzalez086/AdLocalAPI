using AdLocalAPI.Data;
using AdLocalAPI.Interfaces.Tarjetas;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class TarjetaRepository : ITarjetaRepository
    {
        private readonly AppDbContext _context;

        public TarjetaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tarjeta>> GetByUser(long userId)
            => await _context.Tarjeta
                .Where(t => t.UserId == userId && t.Status == true).ToListAsync();                

        public async Task<Tarjeta?> GetById(long id, long userId)
            => await _context.Tarjeta
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        public async Task Add(Tarjeta tarjeta)
            => await _context.Tarjeta.AddAsync(tarjeta);
        public async Task Update(Tarjeta tarjeta)
        {
            _context.Tarjeta.Update(tarjeta);
            await _context.SaveChangesAsync();
        }


        public async Task RemoveDefaults(long userId)
        {
            var tarjetas = await _context.Tarjeta
                .Where(t => t.UserId == userId && t.IsDefault)
                .ToListAsync();

            tarjetas.ForEach(t => t.IsDefault = false);
        }

        public async Task Save()
            => await _context.SaveChangesAsync();
    }

}
