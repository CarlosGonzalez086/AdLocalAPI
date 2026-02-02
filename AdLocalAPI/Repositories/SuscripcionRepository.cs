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



        public async Task CrearAsync(Suscripcion suscripcion)
        {
            _context.Suscripcions.Add(suscripcion);
            await _context.SaveChangesAsync();
        }



        public async Task ActualizarAsync(Suscripcion suscripcion)
        {
            _context.Suscripcions.Update(suscripcion);
            await _context.SaveChangesAsync();
        }


        public async Task<Suscripcion?> GetActivaByUsuario(int usuarioId)
        {
            return await _context.Suscripcions
                .Include(s => s.Plan)
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s =>
                    s.UsuarioId == usuarioId &&
                    s.IsActive &&
                    (s.Status == "active" || s.Status == "canceling") &&
                    s.CurrentPeriodEnd >= DateTime.UtcNow
                );
        }
        public async Task<Suscripcion?> ObtenerActiva(int usuarioId)
        {
            return await _context.Suscripcions
                .Include(s => s.Plan)
                .Where(s =>
                    s.UsuarioId == usuarioId &&
                    s.IsActive &&
                    !s.IsDeleted)
                .OrderByDescending(s => s.CurrentPeriodEnd)
                .FirstOrDefaultAsync();
        }
        public async Task<Suscripcion?> ObtenerPorStripeId(string stripeSubscriptionId)
        {
            return await _context.Suscripcions
                .Include(s => s.Usuario)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s =>
                    s.StripeSubscriptionId == stripeSubscriptionId &&
                    !s.IsDeleted);
        }
        public async Task<List<Suscripcion>> ObtenerHistorial(int usuarioId)
        {
            return await _context.Suscripcions
                .Include(s => s.Plan)
                .Where(s =>
                    s.UsuarioId == usuarioId &&
                    !s.IsDeleted)
                .OrderByDescending(s => s.CurrentPeriodEnd)
                .ToListAsync();
        }
        public async Task EliminarAsync(int suscripcionId)
        {
            var sub = await _context.Suscripcions.FindAsync(suscripcionId);
            if (sub == null) return;

            sub.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistePorSessionAsync(string sessionId)
        {
            return await _context.Suscripcions
                .AnyAsync(x => x.StripeCheckoutSessionId == sessionId);
        }
        public async Task<Suscripcion?> GetActivaByUsuarioAsync(long usuarioId)
        {
            return await _context.Suscripcions
                .Include(s => s.Plan)
                .Where(s =>
                    s.UsuarioId == usuarioId &&
                    !s.IsDeleted &&
                    (
                        (
                            s.Status == "active" &&
                            s.CurrentPeriodEnd >= DateTime.UtcNow
                        )
                        ||
                        (
                            s.Status == "canceling" &&
                            s.CurrentPeriodEnd >= DateTime.UtcNow
                        )
                    )
                )
                .OrderByDescending(s => s.CurrentPeriodEnd)
                .FirstOrDefaultAsync();

        }
        public async Task<List<Suscripcion>> ObtenerParaAutoRenovacionAsync(DateTime fecha)
        {
            return await _context.Suscripcions
                .Include(s => s.Usuario)
                .Include(s => s.Plan)
                .Where(s =>
                    s.IsActive &&
                    s.AutoRenew &&
                    s.CurrentPeriodEnd <= fecha &&
                    s.Usuario.StripeCustomerId != null
                )
                .ToListAsync();
        }


    }
}
