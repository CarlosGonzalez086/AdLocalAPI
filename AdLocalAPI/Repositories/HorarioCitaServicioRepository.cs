using AdLocalAPI.Data;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class HorarioCitaServicioRepository
        : IHorarioCitaServicioRepository
    {
        private readonly AppDbContext _context;

        public HorarioCitaServicioRepository(
            AppDbContext context
        )
        {
            _context = context;
        }

        public async Task<List<HorarioCitaServicio>>
            ObtenerPorServicioFechaAsync(
                long productoServicioId,
                DateOnly fecha
            )
        {
            return await _context.HorariosCitaServicio
                .Where(x =>
                    x.IdProductoServicio == productoServicioId &&
                    x.Fecha == fecha
                )
                .ToListAsync();
        }

        public async Task<List<HorarioCitaServicio>>
            ObtenerDisponiblesAsync(
                long productoServicioId,
                DateOnly fecha
            )
        {
            return await _context.HorariosCitaServicio
                .AsNoTracking()
                .Where(x =>
                    x.IdProductoServicio == productoServicioId &&
                    x.Fecha == fecha &&
                    x.Disponible &&
                    x.IdCita == null
                )
                .OrderBy(x => x.HoraInicio)
                .ToListAsync();
        }

        public async Task<HorarioCitaServicio?> ObtenerDisponibleAsync(
            long productoServicioId,
            DateOnly fecha,
            TimeSpan horaInicio
        )
        {
            return await _context.HorariosCitaServicio
                .FirstOrDefaultAsync(x =>
                    x.IdProductoServicio == productoServicioId &&
                    x.Fecha == fecha &&
                    x.HoraInicio == horaInicio &&
                    x.Disponible &&
                    x.IdCita == null
                );
        }

        public async Task<HorarioCitaServicio?> ObtenerPorCitaAsync(
            long citaId
        )
        {
            return await _context.HorariosCitaServicio
                .FirstOrDefaultAsync(x =>
                    x.IdCita == citaId
                );
        }

        public void Agregar(HorarioCitaServicio horario)
        {
            _context.HorariosCitaServicio.Add(horario);
        }

        public void EliminarRango(
            IEnumerable<HorarioCitaServicio> horarios
        )
        {
            _context.HorariosCitaServicio.RemoveRange(horarios);
        }

        public async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}