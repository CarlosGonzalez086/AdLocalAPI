using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Comercio
{
    public interface IHorarioComercioService
    {
        Task<bool> CrearHorariosAsync(
            int comercioId,
            List<HorarioComercio> horarios
        );
        Task<bool> ActualizarHorariosAsync(
            int comercioId,
            List<HorarioComercio> horarios
        );
        Task<List<HorarioComercio>> ObtenerHorariosPorComercioAsync(
            int comercioId
        );
        Task<bool> EliminarHorariosPorComercioAsync(
            int comercioId
        );
        Task<bool> ComercioTieneHorariosAsync(
            int comercioId
        );
    }
}
