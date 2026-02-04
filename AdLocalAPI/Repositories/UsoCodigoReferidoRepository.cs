using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class UsoCodigoReferidoRepository
    {
        private readonly AppDbContext _context;

        public UsoCodigoReferidoRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<bool> InsertarAsync(
            long usuarioReferidorId,
            long usuarioReferidoId,
            string codigoReferido)
        {
        
            if (usuarioReferidorId == usuarioReferidoId)
                return false;


            bool yaUsoCodigo = await _context.UsoCodigoReferido
                .AnyAsync(x => x.UsuarioReferidoId == usuarioReferidoId);

            if (yaUsoCodigo)
                return false;

            var uso = new UsoCodigoReferido
            {
                UsuarioReferidorId = usuarioReferidorId,
                UsuarioReferidoId = usuarioReferidoId,
                CodigoReferido = codigoReferido,
                FechaUso = DateTime.UtcNow
            };

            _context.UsoCodigoReferido.Add(uso);
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> EliminarAsync(long id)
        {
            var uso = await _context.UsoCodigoReferido
                .FirstOrDefaultAsync(x => x.Id == id);

            if (uso == null)
                return false;

            _context.UsoCodigoReferido.Remove(uso);
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<List<UsoCodigoReferido>> ObtenerPorReferidorAsync(long usuarioReferidorId)
        {
            return await _context.UsoCodigoReferido
                .Where(x => x.UsuarioReferidorId == usuarioReferidorId)
                .OrderByDescending(x => x.FechaUso)
                .ToListAsync();
        }
        public async Task<UsoCodigoReferido?> ObtenerPorUsuarioReferidoAsync(long usuarioReferidoId)
        {
            return await _context.UsoCodigoReferido
                .FirstOrDefaultAsync(x => x.UsuarioReferidoId == usuarioReferidoId);
        }
        public async Task<List<UsoCodigoReferido>> ObtenerTodosAsync()
        {
            return await _context.UsoCodigoReferido
                .OrderByDescending(x => x.FechaUso)
                .ToListAsync();
        }
        public async Task<int> ContarPorReferidorAsync(long usuarioReferidorId)
        {
            return await _context.UsoCodigoReferido
                .Where(x => x.UsuarioReferidorId == usuarioReferidorId)
                .CountAsync();
        }

        public async Task<int> ContarPorCodigoAsync(string codigoReferido)
        {
            return await _context.UsoCodigoReferido
                .Where(x => x.CodigoReferido == codigoReferido)
                .CountAsync();
        }

        public async Task<int> ContarTotalAsync()
        {
            return await _context.UsoCodigoReferido.CountAsync();
        }

    }
}
