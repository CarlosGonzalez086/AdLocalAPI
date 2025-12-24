using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class SuscripcionRepository
    {
        private readonly AppDbContext _context;

        public SuscripcionRepository(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Crear suscripción
        public async Task<Suscripcion> CreateAsync(Suscripcion suscripcion)
        {
            _context.Suscripcions.Add(suscripcion);
            await _context.SaveChangesAsync();
            return suscripcion;
        }

        // 🔹 Actualizar suscripción
        public async Task UpdateAsync(Suscripcion suscripcion)
        {
            _context.Suscripcions.Update(suscripcion);
            await _context.SaveChangesAsync();
        }

        // 🔹 Obtener suscripción activa por usuario
        public async Task<Suscripcion?> GetActivaByUsuario(int usuarioId)
        {
            return await _context.Suscripcions
                .Include(s => s.Plan)
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s =>
                    s.UsuarioId == usuarioId &&
                    s.Activa &&
                    s.Estado == "Activa" &&
                    s.FechaFin >= DateTime.UtcNow
                );
        }

        // 🔹 Obtener todas las suscripciones (Admin)
        public async Task<List<Suscripcion>> GetAllAsync()
        {
            return await _context.Suscripcions
                .Include(s => s.Plan)
                .Include(s => s.Usuario)
                .OrderByDescending(s => s.FechaCreacion)
                .ToListAsync();
        }

        // 🔹 Obtener suscripción por Id
        public async Task<Suscripcion?> GetByIdAsync(int id)
        {
            return await _context.Suscripcions
                .Include(s => s.Plan)
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        // 🔹 Cancelar suscripción (Admin o Usuario)
        public async Task CancelarAsync(Suscripcion suscripcion)
        {
            suscripcion.Activa = false;
            suscripcion.Estado = "Cancelada";
            suscripcion.FechaFin = DateTime.UtcNow;

            _context.Suscripcions.Update(suscripcion);
            await _context.SaveChangesAsync();
        }

        // 🔹 Expirar suscripciones vencidas (CRON / Background Job)
        public async Task ExpirarSuscripciones()
        {
            var vencidas = await _context.Suscripcions
                .Where(s => s.Activa && s.FechaFin < DateTime.UtcNow)
                .ToListAsync();

            foreach (var s in vencidas)
            {
                s.Activa = false;
                s.Estado = "Expirada";
            }

            await _context.SaveChangesAsync();
        }
        public async Task<Suscripcion?> GetByUsuarioId(int usuarioId)
        {
            return await _context.Suscripcions
                .Where(s => s.UsuarioId == usuarioId && s.Activa)
                .OrderByDescending(s => s.FechaFin)
                .FirstOrDefaultAsync();
        }
    }
}
