//using AdLocalAPI.Data;
//using AdLocalAPI.Models;
//using AdLocalAPI.Repositories;
//using Microsoft.EntityFrameworkCore;

//namespace AdLocalAPI.Services
//{
//    /// <summary>
//    /// Servicio automático para sincronizar el estado de las suscripciones.
//    /// - Cierra suscripciones vencidas
//    /// - Garantiza que los usuarios tengan al menos un plan FREE
//    /// </summary>
//    public class SuscripcionAutoService
//    {
//        private readonly AppDbContext _context;
//        private readonly SuscripcionRepository _suscripcionRepo;

//        public SuscripcionAutoService(
//            AppDbContext context,
//            SuscripcionRepository suscripcionRepo
//        )
//        {
//            _context = context;
//            _suscripcionRepo = suscripcionRepo;
//        }

//        public async Task SincronizarSuscripcionesAsync()
//        {
//            var hoy = DateTime.UtcNow;

//            var planFree = await _context.Plans
//                .AsNoTracking()
//                .FirstOrDefaultAsync(p => p.Tipo == "FREE");

//            if (planFree == null)
//                return;

//            var suscripcionesVencidas = await _context.Suscripcions
//                .Where(s =>
//                    s.Activa &&
//                    s.Estado == "active" &&
//                    s.FechaFin < hoy
//                )
//                .ToListAsync();

//            foreach (var s in suscripcionesVencidas)
//            {
//                s.Activa = false;
//                s.Estado = "completed";
//                s.AutoRenovacion = false;
//                s.FechaFin = hoy;

//                await _suscripcionRepo.ActualizarAsync(s);
//            }

//            var usuariosComercio = await _context.Usuarios
//                .Where(u => u.Rol == "COMERCIO" && u.Activo)
//                .Select(u => u.Id)
//                .ToListAsync();

//            var usuariosConSuscripcionActiva = await _context.Suscripcions
//                .Where(s =>
//                    s.Activa &&
//                    s.Estado == "active" &&
//                    s.FechaFin > hoy
//                )
//                .Select(s => s.UsuarioId)
//                .Distinct()
//                .ToListAsync();

//            var usuariosSinSuscripcion = usuariosComercio
//                .Except(usuariosConSuscripcionActiva)
//                .ToList();

//            var usuariosConFree = await _context.Suscripcions
//                .Where(s =>
//                    s.Activa &&
//                    s.PlanId == planFree.Id
//                )
//                .Select(s => s.UsuarioId)
//                .Distinct()
//                .ToListAsync();

//            foreach (var usuarioId in usuariosSinSuscripcion.Except(usuariosConFree))
//            {
//                var free = new Suscripcion
//                {
//                    UsuarioId = usuarioId,
//                    PlanId = planFree.Id,
//                    Activa = true,
//                    Estado = "active",
//                    FechaInicio = hoy,
//                    FechaFin = DateTime.MaxValue
//                };

//                await _suscripcionRepo.CrearAsync(free);
//            }

//            await _context.SaveChangesAsync();
//        }
//    }
//}
