using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Comercio
{
    public interface IHorarioComercioService
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
    }
}
