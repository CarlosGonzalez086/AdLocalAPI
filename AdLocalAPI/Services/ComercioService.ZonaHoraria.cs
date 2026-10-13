using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services;

public partial class ComercioService
{
    public async Task<ApiResponse<string>> ActualizarZonaHorariaAsync(long id, ComercioZonaHorariaDto dto, CancellationToken ct = default)
    {
        try { return ApiResponse<string>.Success(await _repository.ActualizarZonaHorariaAsync(id, _jwtContext.GetUserId(), dto, ct)); }
        catch (CitaOperacionException e) { return ApiResponse<string>.Error(e.Codigo, e.Message); }
        catch { return ApiResponse<string>.Error("500", "No se confirmó la zona horaria. Actualiza el comercio antes de reintentar."); }
    }
}
