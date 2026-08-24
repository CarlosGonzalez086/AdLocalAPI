using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IHorarioCitaServicioRepository
    {
        Task<List<HorarioCitaServicio>> ObtenerPorServicioFechaAsync(
            long productoServicioId,
            DateOnly fecha
        );

        Task<List<HorarioCitaServicio>> ObtenerDisponiblesAsync(
            long productoServicioId,
            DateOnly fecha
        );

        Task<HorarioCitaServicio?> ObtenerDisponibleAsync(
            long productoServicioId,
            DateOnly fecha,
            TimeSpan horaInicio
        );

        Task<HorarioCitaServicio?> ObtenerPorCitaAsync(
            long citaId
        );

        void Agregar(HorarioCitaServicio horario);

        void EliminarRango(
            IEnumerable<HorarioCitaServicio> horarios
        );

        Task GuardarCambiosAsync();
    }
}