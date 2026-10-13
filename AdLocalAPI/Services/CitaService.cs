using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using Npgsql;

namespace AdLocalAPI.Services;

public partial class CitaService(ICitaRepository citaRepository, IComercioRepository comercioRepository, JwtContext jwt,
    ILogger<CitaService> logger) : ICitaService
{
    private readonly ICitaRepository _citaRepository = citaRepository;
    private readonly IComercioRepository _comercioRepository = comercioRepository;
    private readonly JwtContext _jwt = jwt;
    private readonly ILogger<CitaService> _logger = logger;
    public async Task<ApiResponse<List<string>>> HorariosAsync(Guid productoUuid, DateOnly fecha, Guid? citaUuid = null) =>
        await EjecutarAsync(() => _citaRepository.DisponibilidadAsync(productoUuid, fecha, _jwt.GetUserId(), citaUuid));
    public async Task<ApiResponse<List<CitaDto>>> MisCitasAsync()
    {
        var datos = await _citaRepository.ObtenerPorUsuarioAsync(_jwt.GetUserId());
        var ahora = DateTime.UtcNow;
        return ApiResponse<List<CitaDto>>.Success(datos.OrderBy(x => CitaOperacion.Instante(x.FechaInicio, x.ZonaHoraria) < ahora || x.Estado == EstadoCita.Cancelada)
            .ThenBy(x => CitaOperacion.Instante(x.FechaInicio, x.ZonaHoraria) >= ahora && x.Estado != EstadoCita.Cancelada ? CitaOperacion.Instante(x.FechaInicio, x.ZonaHoraria) : DateTime.MaxValue)
            .ThenByDescending(x => CitaOperacion.Instante(x.FechaInicio, x.ZonaHoraria)).ToList());
    }
    public async Task<ApiResponse<List<CitaDto>>> AgendaAsync(long comercioId, DateOnly? fecha)
    {
        if (!await PuedeAdministrarAsync(comercioId)) return ApiResponse<List<CitaDto>>.Error("403", "No tienes acceso a este comercio.");
        return ApiResponse<List<CitaDto>>.Success(await _citaRepository.ObtenerAgendaAsync(comercioId, fecha));
    }
    private Task<bool> PuedeAdministrarAsync(long comercioId) => _comercioRepository.PuedeAdministrarAsync(comercioId, _jwt.GetUserId());
    private async Task<ApiResponse<T>> EjecutarAsync<T>(Func<Task<T>> operacion)
    {
        try { return ApiResponse<T>.Success(await operacion()); }
        catch (CitaOperacionException e) { return ApiResponse<T>.Error(e.Codigo, e.Message); }
        catch (Exception e) when (EsConflicto(e)) { return ApiResponse<T>.Error("409", "La agenda cambió. Actualiza la cita y comprueba su horario."); }
        catch (Exception e) { _logger.LogError(e, "Fallo al operar citas"); return ApiResponse<T>.Error("500", "No se confirmó la operación. Revisa tus citas antes de reintentar."); }
    }
    private static bool EsConflicto(Exception e)
    {
        for (Exception? actual = e; actual != null; actual = actual.InnerException)
            if (actual is PostgresException { SqlState: "23P01" or "23505" or "40001" or "40P01" }) return true;
        return false;
    }
    private async Task<CitaDto> RespuestaAsync(Models.Cita cita) => await _citaRepository.ObtenerDtoAsync(cita.Id)
        ?? throw new InvalidOperationException("No se pudo consultar la cita persistida.");
}
