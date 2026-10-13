using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services;

public partial class CitaService
{
    public Task<ApiResponse<CitaDto>> CrearAsync(CrearCitaDto dto, CancellationToken cancellationToken = default) =>
        EjecutarAsync(async () => await RespuestaAsync(await _citaRepository.ReservarAsync(_jwt.GetUserId(), dto, cancellationToken)));
    public Task<ApiResponse<CitaDto>> CancelarClienteAsync(Guid uuid, CancelarCitaDto dto) =>
        EjecutarAsync(async () => await RespuestaAsync(await _citaRepository.OperarAsync(uuid, _jwt.GetUserId(), null,
            dto.VersionCita, EstadoCita.Cancelada, null, dto.Motivo, null)));
    public Task<ApiResponse<CitaDto>> ReprogramarClienteAsync(Guid uuid, ReprogramarCitaDto dto) =>
        EjecutarAsync(async () => await RespuestaAsync(await _citaRepository.OperarAsync(uuid, _jwt.GetUserId(), null,
            dto.VersionCita, null, null, null, dto.FechaInicio, zonaHoraria: dto.ZonaHoraria)));
    public async Task<ApiResponse<CitaDto>> ActualizarAsync(long comercioId, Guid uuid, ActualizarCitaComercioDto dto)
    {
        if (!await PuedeAdministrarAsync(comercioId)) return ApiResponse<CitaDto>.Error("403", "No tienes acceso a este comercio.");
        return await EjecutarAsync(async () => await RespuestaAsync(await _citaRepository.OperarAsync(uuid, _jwt.GetUserId(), comercioId,
            dto.VersionCita, dto.Estado, dto.NombreAtiende, dto.Motivo, null)));
    }
}
