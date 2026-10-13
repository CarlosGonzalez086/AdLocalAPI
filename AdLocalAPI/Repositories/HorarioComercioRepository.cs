using AdLocalAPI.Data;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using AdLocalAPI.Helpers;

namespace AdLocalAPI.Repositories
{
    public class HorarioComercioRepository : IHorarioComercioRepository
    {
        private readonly AppDbContext _context;

        public HorarioComercioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CrearHorariosAsync(
            long comercioId,
            List<HorarioComercio> horarios)
        {
            if (horarios == null || !horarios.Any())
                return false;

            return await GuardarAsync(comercioId, horarios, true);
        }
        public async Task<bool> ActualizarHorariosAsync(
            long comercioId,
            List<HorarioComercio> horarios)
            => await GuardarAsync(comercioId, horarios, false);

        private async Task<bool> GuardarAsync(long comercioId, List<HorarioComercio> horarios, bool crear)
        {
            CitaOperacion.Exigir(horarios.Select(h => h.Dia).Distinct().Count() == horarios.Count && horarios.All(h =>
                Enum.IsDefined(h.Dia) && (!h.Abierto || h.HoraApertura.HasValue && h.HoraCierre.HasValue &&
                h.HoraApertura >= TimeSpan.Zero && h.HoraCierre < TimeSpan.FromDays(1) && h.HoraCierre > h.HoraApertura &&
                h.HoraApertura.Value.Ticks % TimeSpan.TicksPerMinute == 0 && h.HoraCierre.Value.Ticks % TimeSpan.TicksPerMinute == 0)), "Horarios inválidos o días repetidos.", "400");
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.Comercios.FromSqlInterpolated($"SELECT * FROM \"Comercios\" WHERE \"Id\" = {comercioId} FOR UPDATE").AsNoTracking().SingleAsync();
                var existentes = await _context.HorarioComercio
                    .Where(h => h.ComercioId == comercioId)
                    .ToListAsync();

                if (crear && existentes.Count > 0) { await transaction.RollbackAsync(); return false; }
                var pendientes = await _context.Citas.AsNoTracking().Where(c => c.IdComercio == comercioId &&
                    (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada || c.Estado == EstadoCita.EnAtencion)).ToListAsync();
                CitaOperacion.Exigir(pendientes.Where(c => c.FechaFin > CitaOperacion.Ahora(TimeProvider.System, c.ZonaHoraria)).All(c => horarios.Any(h => h.Dia == c.FechaInicio.DayOfWeek && h.Abierto &&
                    h.HoraApertura <= c.FechaInicio.TimeOfDay && h.HoraCierre >= c.FechaFin.TimeOfDay && c.FechaInicio.Date == c.FechaFin.Date)),
                    "El nuevo horario deja citas vigentes fuera de atención. Reprograma o cancela esas citas primero.");

                _context.HorarioComercio.RemoveRange(existentes);

                foreach (var h in horarios)
                {
                    h.ComercioId = comercioId;
                }

                await _context.HorarioComercio.AddRangeAsync(horarios);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<List<HorarioComercio>> ObtenerHorariosPorComercioAsync(
            long comercioId)
        {
            return await _context.HorarioComercio
                .Where(h => h.ComercioId == comercioId)
                .OrderBy(h => h.Dia)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<bool> EliminarHorariosPorComercioAsync(
            long comercioId)
        {
            return await GuardarAsync(comercioId, [], false);
        }

        public async Task<bool> ComercioTieneHorariosAsync(
            long comercioId)
        {
            return await _context.HorarioComercio
                .AnyAsync(h => h.ComercioId == comercioId);
        }
        public async Task<HorarioComercio?> ObtenerAsync(long comercioId,DayOfWeek dia)
        {
            return await _context.HorarioComercio
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ComercioId == comercioId &&
                    x.Dia == dia
                );
        }
    }
}
