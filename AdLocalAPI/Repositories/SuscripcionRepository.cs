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

        public async Task CrearAsync(Suscripcion suscripcion)
        {
            _context.Suscripcions.Add(suscripcion);
            await _context.SaveChangesAsync();
        }


        // 🔹 Actualizar suscripción
        public async Task ActualizarAsync(Suscripcion suscripcion)
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
                    s.Estado == "active" &&
                    s.FechaFin >= DateTime.UtcNow
                );
        }
        public async Task<Suscripcion?> ObtenerActiva(int usuarioId)
        {
            return await _context.Suscripcions
                .Include(s => s.Plan)
                .Where(s =>
                    s.UsuarioId == usuarioId &&
                    s.Activa &&
                    !s.Eliminada)
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefaultAsync();
        }
        public async Task<Suscripcion?> ObtenerPorStripeId(string stripeSubscriptionId)
        {
            return await _context.Suscripcions
                .Include(s => s.Usuario)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s =>
                    s.StripeSubscriptionId == stripeSubscriptionId &&
                    !s.Eliminada);
        }
        public async Task<List<Suscripcion>> ObtenerHistorial(int usuarioId)
        {
            return await _context.Suscripcions
                .Include(s => s.Plan)
                .Where(s =>
                    s.UsuarioId == usuarioId &&
                    !s.Eliminada)
                .OrderByDescending(s => s.FechaInicio)
                .ToListAsync();
        }
        public async Task EliminarAsync(int suscripcionId)
        {
            var sub = await _context.Suscripcions.FindAsync(suscripcionId);
            if (sub == null) return;

            sub.Eliminada = true;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistePorSessionAsync(string sessionId)
        {
            return await _context.Suscripcions
                .AnyAsync(x => x.StripeSessionId == sessionId);
        }
        public async Task<Suscripcion?> GetActivaByUsuarioAsync(long usuarioId)
        {
            return await _context.Suscripcions
                .Include(s => s.Plan)
                .Where(s =>
                    s.UsuarioId == usuarioId &&
                    s.Activa &&
                    !s.Eliminada &&
                    s.Estado == "active" &&
                    s.FechaInicio <= DateTime.UtcNow &&
                    s.FechaFin >= DateTime.UtcNow
                )
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefaultAsync();
        }

    }
}
