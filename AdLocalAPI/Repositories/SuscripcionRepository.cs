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
            try
            {
                _context.Suscripcions.Add(suscripcion);
                await _context.SaveChangesAsync();
                return suscripcion;
            }
            catch (DbUpdateException ex)
            {
                // 🔐 Stripe puede mandar el mismo evento varias veces
                // Si ya existe, NO fallamos el webhook
                throw new InvalidOperationException(
                    "Error al guardar la suscripción. Posible duplicado o violación de integridad.",
                    ex
                );
            }
            catch (Exception ex)
            {
                // ❌ Error inesperado
                throw new ApplicationException(
                    "Error inesperado al crear la suscripción.",
                    ex
                );
            }
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
                    s.Estado == "active" &&
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
        public async Task<bool> ExistePorSessionAsync(string sessionId)
        {
            return await _context.Suscripcions
                .AnyAsync(x => x.StripeSessionId == sessionId);
        }

    }
}
