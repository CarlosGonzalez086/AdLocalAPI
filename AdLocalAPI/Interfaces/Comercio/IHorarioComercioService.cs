using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Comercio
{
    public interface IHorarioComercioRepository
    {
        Task<bool> CrearHorariosAsync(
            long comercioId,
            List<HorarioComercio> horarios
        );
        Task<bool> ActualizarHorariosAsync(
            long comercioId,
            List<HorarioComercio> horarios
        );
        Task<List<HorarioComercio>> ObtenerHorariosPorComercioAsync(
            long comercioId
        );
        Task<bool> EliminarHorariosPorComercioAsync(
            long comercioId
        );
        Task<bool> ComercioTieneHorariosAsync(
            long comercioId
        );
        Task<HorarioComercio?> ObtenerAsync(long comercioId, DayOfWeek dia);
    }
}
