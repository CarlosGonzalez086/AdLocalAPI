using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface ICitaRepository
    {
        Task<List<string>> DisponibilidadAsync(Guid producto, DateOnly fecha, long usuario, Guid? cita, CancellationToken ct = default);
        Task<Cita> ReservarAsync(long usuario, CrearCitaDto dto, CancellationToken ct);
        Task<Cita> OperarAsync(Guid uuid, long actor, long? comercio, long version, EstadoCita? estado,
            string? atiende, string? motivo, DateTime? inicio, CancellationToken ct = default, string? zonaHoraria = null);
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

    }
}
