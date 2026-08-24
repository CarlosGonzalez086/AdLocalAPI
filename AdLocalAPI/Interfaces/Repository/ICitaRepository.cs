using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface ICitaRepository
    {
        Task<Cita?> ObtenerPorIdAsync(long id);

        Task<Cita?> ObtenerPorUuidClienteAsync(
            Guid uuid,
            long usuarioId
        );

        Task<Cita?> ObtenerPorUuidComercioAsync(
            Guid uuid,
            long comercioId
        );

        Task<List<Cita>> ObtenerOcupadasAsync(
            long comercioId,
            DateTime inicio,
            DateTime fin
        );

        Task<List<CitaDto>> ObtenerPorUsuarioAsync(
            long usuarioId
        );

        Task<List<CitaDto>> ObtenerAgendaAsync(
            long comercioId,
            DateOnly? fecha
        );

        Task<CitaDto?> ObtenerDtoAsync(long id);

        Task<Cita> CrearAsync(Cita cita);

        Task GuardarCambiosAsync(Cita cita);
    }
}