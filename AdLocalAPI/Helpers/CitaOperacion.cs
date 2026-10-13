using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Helpers;

public sealed class CitaOperacionException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

public static class CitaOperacion
{
    // Default only; each commerce and appointment carries its own IANA timezone.
    public const string ZonaHoraria = "America/Mexico_City";
    public static TimeZoneInfo Zona(string nombre)
    {
        Exigir(!string.IsNullOrWhiteSpace(nombre) && nombre.Length <= 100 && nombre.Contains('/'), "Selecciona una zona horaria IANA válida.", "400");
        try { return TimeZoneInfo.FindSystemTimeZoneById(nombre); }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException) { throw new CitaOperacionException("400", "Zona horaria no reconocida."); }
    }
    public static bool ZonaValida(string nombre) { try { Zona(nombre); return true; } catch (CitaOperacionException) { return false; } }
    public static DateTime Ahora(TimeProvider reloj, string nombre = ZonaHoraria) => DateTime.SpecifyKind(
        TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), Zona(nombre)).DateTime, DateTimeKind.Unspecified);
    public static bool IntervaloValido(DateTime inicio, DateTime fin, string nombre)
    {
        var zona = Zona(nombre);
        return !zona.IsInvalidTime(inicio) && !zona.IsAmbiguousTime(inicio) && !zona.IsInvalidTime(fin) && !zona.IsAmbiguousTime(fin) &&
            TimeZoneInfo.ConvertTimeToUtc(fin, zona) - TimeZoneInfo.ConvertTimeToUtc(inicio, zona) == fin - inicio;
    }
    public static DateTime Instante(DateTime fecha, string nombre) => TimeZoneInfo.ConvertTimeToUtc(fecha, Zona(nombre));
    public static void Exigir(bool condicion, string mensaje, string codigo = "409")
    { if (!condicion) throw new CitaOperacionException(codigo, mensaje); }
    public static void Fecha(DateTime inicio, DateTime ahora)
    {
        Exigir(inicio.Kind == DateTimeKind.Unspecified && inicio.Ticks % TimeSpan.TicksPerMinute == 0,
            "Envía fecha y hora local del comercio, sin zona ni segundos adicionales.", "400");
        Exigir(inicio > ahora && inicio < ahora.Date.AddDays(366), "Selecciona un horario futuro dentro de los próximos 365 días.", "400");
    }
    public static List<DateTime> Espacios(ProductosServicios servicio, HorarioComercio? horario, DateOnly fecha)
    {
        if (horario is not { Abierto: true, HoraApertura: not null, HoraCierre: not null } ||
            servicio.DuracionMinutos is not (> 0 and <= 1440)) return [];
        var apertura = horario.HoraApertura.Value; var cierre = horario.HoraCierre.Value;
        if (apertura < TimeSpan.Zero || cierre >= TimeSpan.FromDays(1) || cierre <= apertura ||
            apertura.Ticks % TimeSpan.TicksPerMinute != 0 || cierre.Ticks % TimeSpan.TicksPerMinute != 0) return [];
        var dia = fecha.ToDateTime(TimeOnly.MinValue); var duracion = TimeSpan.FromMinutes(servicio.DuracionMinutos.Value);
        List<DateTime> resultado = [];
        for (var hora = apertura; hora + duracion <= cierre; hora += duracion) resultado.Add(dia + hora);
        return resultado;
    }
    public static List<EstadoCita> Acciones(EstadoCita estado) => estado switch
    {
        EstadoCita.Pendiente => [EstadoCita.Confirmada, EstadoCita.Cancelada, EstadoCita.NoAsistio],
        EstadoCita.Confirmada => [EstadoCita.EnAtencion, EstadoCita.Cancelada, EstadoCita.NoAsistio],
        EstadoCita.EnAtencion => [EstadoCita.Completada],
        _ => []
    };
    public static List<EstadoCita> Acciones(Cita cita, DateTime ahora) => Acciones(cita.Estado).Where(estado =>
        (estado is not (EstadoCita.EnAtencion or EstadoCita.NoAsistio) || ahora >= cita.FechaInicio) &&
        (estado != EstadoCita.Completada || ahora >= cita.FechaFin)).ToList();
    public static string Hash(CrearCitaDto dto) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new { dto.ProductoUuid, Fecha = dto.FechaInicio.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            dto.ZonaHoraria, Nombre = dto.NombrePersona.Trim(), Notas = dto.Notas?.Trim() ?? "" }))));
}
