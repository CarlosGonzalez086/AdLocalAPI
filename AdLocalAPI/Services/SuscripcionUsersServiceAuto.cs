using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Services
{
    public class SuscripcionUsersServiceAuto
    {
        private readonly AppDbContext _context;

        public SuscripcionUsersServiceAuto(AppDbContext context)
        {
            _context = context;
        }

        public async Task AsignarPlanFreeAComerciosSinSuscripcion()
        {
            var hoy = DateTime.UtcNow;

            var planFree = await _context.Plans
                .FirstOrDefaultAsync(p => p.Tipo == "FREE");

            if (planFree == null)
                return;


            var usuariosComercio = await _context.Usuarios
                .Where(u => u.Rol == "COMERCIO" && u.Activo)
                .Select(u => u.Id)
                .ToListAsync();


            var usuariosConSuscripcionActiva = await _context.Suscripcions
                .Where(s =>
                    s.Activa &&
                    s.Estado == "active" &&
                    s.FechaFin > hoy
                )
                .Select(s => s.UsuarioId)
                .ToListAsync();


            var usuariosSinSuscripcion = usuariosComercio
                .Except(usuariosConSuscripcionActiva)
                .ToList();

            foreach (var usuarioId in usuariosSinSuscripcion)
            {

                var suscripcionesVencidas = await _context.Suscripcions
                    .Where(s =>
                        s.UsuarioId == usuarioId &&
                        s.Activa &&
                        s.FechaFin < hoy
                    )
                    .ToListAsync();

                foreach (var suscripcion in suscripcionesVencidas)
                {
                    suscripcion.Activa = false;
                    suscripcion.Estado = "finalizada";
                }

                bool yaTieneFree = await _context.Suscripcions.AnyAsync(s =>
                    s.UsuarioId == usuarioId &&
                    s.Activa &&
                    s.PlanId == planFree.Id
                );

                if (!yaTieneFree)
                {
                    var free = new Suscripcion
                    {
                        UsuarioId = usuarioId,
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
