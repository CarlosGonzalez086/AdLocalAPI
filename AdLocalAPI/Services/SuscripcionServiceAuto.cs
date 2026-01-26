using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Services
{
    public class SuscripcionServiceAuto
    {
        private readonly AppDbContext _context;

        public SuscripcionServiceAuto(AppDbContext context)
        {
            _context = context;
        }

        public async Task ProcesarSuscripcionesVencidas()
        {
            var hoy = DateTime.UtcNow;

            var planFree = await _context.Plans
                .FirstOrDefaultAsync(x => x.Tipo == "FREE");

            if (planFree == null)
                return;

            var vencidas = await _context.Suscripcions
                .Where(s =>
                    s.Activa &&                       
                    s.Estado == "active" &&
                    s.FechaFin < hoy
                )
                .ToListAsync();

            foreach (var suscripcion in vencidas)
            {

                suscripcion.Activa = false;
                suscripcion.Estado = "finalizada";

                _context.Suscripcions.Update(suscripcion);
                bool yaTieneFree = await _context.Suscripcions.AnyAsync(s =>
                    s.UsuarioId == suscripcion.UsuarioId &&
                    s.Activa &&
                    s.PlanId == planFree.Id
                );

                if (!yaTieneFree)
                {
                    var free = new Suscripcion
                    {
                        UsuarioId = suscripcion.UsuarioId,
                        PlanId = planFree.Id,
                        Activa = true,
                        Estado = "active",
                        FechaInicio = hoy,
                        FechaFin = DateTime.MaxValue
                    };

                    _context.Suscripcions.Add(free);
                }
            }

            await _context.SaveChangesAsync();
        }

    }
}
