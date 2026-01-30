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

        /// <summary>
        /// Inserta el uso de un código referido
        /// </summary>
        public async Task<bool> InsertarAsync(
            long usuarioReferidorId,
            long usuarioReferidoId,
            string codigoReferido)
        {
            // No permitir auto-referido
            if (usuarioReferidorId == usuarioReferidoId)
                return false;

            // El usuario referido no puede usar código 2 veces
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

        /// <summary>
        /// Elimina un uso de código por Id
        /// </summary>
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

        /// <summary>
        /// Obtiene la lista de usos por el usuario que dio el código
        /// </summary>
        public async Task<List<UsoCodigoReferido>> ObtenerPorReferidorAsync(long usuarioReferidorId)
        {
            return await _context.UsoCodigoReferido
                .Where(x => x.UsuarioReferidorId == usuarioReferidorId)
                .OrderByDescending(x => x.FechaUso)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene un uso específico por usuario referido
        /// </summary>
        public async Task<UsoCodigoReferido?> ObtenerPorUsuarioReferidoAsync(long usuarioReferidoId)
        {
            return await _context.UsoCodigoReferido
                .FirstOrDefaultAsync(x => x.UsuarioReferidoId == usuarioReferidoId);
        }
        /// <summary>
        /// Obtiene todos los usos de códigos referidos
        /// (admin / reportes / estadísticas)
        /// </summary>
        public async Task<List<UsoCodigoReferido>> ObtenerTodosAsync()
        {
            return await _context.UsoCodigoReferido
                .OrderByDescending(x => x.FechaUso)
                .ToListAsync();
        }
    }
}
