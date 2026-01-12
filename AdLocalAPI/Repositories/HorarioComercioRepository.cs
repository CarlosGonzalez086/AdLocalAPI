using AdLocalAPI.Data;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class HorarioComercioRepository : IHorarioComercioService
    {
        private readonly AppDbContext _context;

        public HorarioComercioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CrearHorariosAsync(
            int comercioId,
            List<HorarioComercio> horarios)
        {
            if (horarios == null || !horarios.Any())
                return false;

            if (await ComercioTieneHorariosAsync(comercioId))
                return false;

            foreach (var h in horarios)
            {
                h.ComercioId = comercioId;
            }

            await _context.HorarioComercio.AddRangeAsync(horarios);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> ActualizarHorariosAsync(
            int comercioId,
            List<HorarioComercio> horarios)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existentes = await _context.HorarioComercio
                    .Where(h => h.ComercioId == comercioId)
                    .ToListAsync();

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
                return false;
            }
        }
        public async Task<List<HorarioComercio>> ObtenerHorariosPorComercioAsync(
            int comercioId)
        {
            return await _context.HorarioComercio
                .Where(h => h.ComercioId == comercioId)
                .OrderBy(h => h.Dia)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<bool> EliminarHorariosPorComercioAsync(
            int comercioId)
        {
            var horarios = await _context.HorarioComercio
                .Where(h => h.ComercioId == comercioId)
                .ToListAsync();

            if (!horarios.Any())
                return false;

            _context.HorarioComercio.RemoveRange(horarios);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> ComercioTieneHorariosAsync(
            int comercioId)
        {
            return await _context.HorarioComercio
                .AnyAsync(h => h.ComercioId == comercioId);
        }
    }
}
