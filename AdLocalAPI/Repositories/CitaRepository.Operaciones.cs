using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories;

public partial class CitaRepository
{
    private async Task<(ProductosServicios servicio, string zona)> ServicioAsync(Guid uuid, CancellationToken ct)
    {
        var producto = await _context.ProductosServicios.AsNoTracking().SingleOrDefaultAsync(x => x.Uuid == uuid, ct);
        if (producto == null) throw new CitaOperacionException("404", "Servicio no encontrado.");
        // Shared with opening-hours writes; product edits cannot change duration during this reservation.
        var comercio = await _context.Comercios.FromSqlInterpolated($"SELECT * FROM \"Comercios\" WHERE \"Id\" = {producto.IdComercio} FOR UPDATE")
            .AsNoTracking().SingleOrDefaultAsync(ct);
        producto = await _context.ProductosServicios.FromSqlInterpolated($"SELECT *, xmin FROM \"ProductosServicios\" WHERE id = {producto.Id} FOR SHARE")
            .AsNoTracking().SingleOrDefaultAsync(ct);
        CitaOperacion.Exigir(comercio is { Activo: true, Visible: true } && producto is { Activo: true, Visible: true, Disponible: true,
            Tipo: TipoProductoServicio.Servicio, Modalidad: ModalidadProductoServicio.Reservacion }, "El servicio ya no admite reservas.");
        return (producto!, comercio!.ZonaHoraria);
    }
    public async Task<List<string>> DisponibilidadAsync(Guid producto, DateOnly fecha, long usuario, Guid? cita, CancellationToken ct = default)
    {
        // GET is read-only; the mutation always rechecks this snapshot under the commerce lock.
        var servicio = await _context.ProductosServicios.AsNoTracking().SingleOrDefaultAsync(x => x.Uuid == producto, ct);
        if (servicio == null) throw new CitaOperacionException("404", "Servicio no encontrado.");
        var comercio = await _context.Comercios.AsNoTracking().SingleOrDefaultAsync(x => x.Id == servicio.IdComercio, ct);
        if (comercio == null) return [];
        var zona = comercio.ZonaHoraria;
        var ahora = CitaOperacion.Ahora(_reloj, zona);
        if (fecha < DateOnly.FromDateTime(ahora) || fecha > DateOnly.FromDateTime(ahora).AddDays(365)) return [];
        if (!servicio.Activo || !servicio.Visible || !servicio.Disponible || servicio.Tipo != TipoProductoServicio.Servicio ||
            servicio.Modalidad != ModalidadProductoServicio.Reservacion ||
            !comercio.Activo || !comercio.Visible) return [];
        long? excluir = null;
        if (cita.HasValue)
        {
            var propia = await _context.Citas.AsNoTracking().SingleOrDefaultAsync(x => x.Uuid == cita && x.IdUsuario == usuario && x.IdProductoServicio == servicio.Id, ct);
            if (propia == null) throw new CitaOperacionException("404", "Cita no encontrada.");
            CitaOperacion.Exigir(propia.ZonaHoraria == zona, "La zona del comercio cambió. Revisa la cita antes de reprogramar.");
            CitaOperacion.Exigir(propia.Estado is EstadoCita.Pendiente or EstadoCita.Confirmada && propia.FechaInicio > ahora, "Esta cita ya no se puede reprogramar.");
            excluir = propia.Id;
        }
        var horario = await _context.HorarioComercio.AsNoTracking().SingleOrDefaultAsync(x => x.ComercioId == servicio.IdComercio && x.Dia == fecha.DayOfWeek, ct);
        var desde = fecha.ToDateTime(TimeOnly.MinValue); var hasta = desde.AddDays(1);
        var ocupadas = await _context.Citas.AsNoTracking().Where(x => x.IdComercio == servicio.IdComercio && x.Id != excluir &&
            x.Estado != EstadoCita.Cancelada && x.FechaInicio < hasta && x.FechaFin > desde).ToListAsync(ct);
        return CitaOperacion.Espacios(servicio, horario, fecha).Where(inicio => inicio > ahora &&
            CitaOperacion.IntervaloValido(inicio, inicio.AddMinutes(servicio.DuracionMinutos!.Value), zona) &&
            !ocupadas.Any(c => c.FechaInicio < inicio.AddMinutes(servicio.DuracionMinutos!.Value) && c.FechaFin > inicio))
            .Select(x => x.ToString("HH:mm")).ToList();
    }
    public async Task<Cita> ReservarAsync(long usuario, CrearCitaDto dto, CancellationToken ct)
    {
        CitaOperacion.Exigir(dto.IdempotencyKey != Guid.Empty && dto.ProductoUuid != Guid.Empty, "Indica una clave de reserva y servicio válidos.", "400");
        CitaOperacion.Exigir(!string.IsNullOrWhiteSpace(dto.NombrePersona) && dto.NombrePersona.Trim().Length is >= 2 and <= 100 &&
            dto.Notas?.Length is not > 500, "Indica un nombre válido y notas de hasta 500 caracteres.", "400");
        CitaOperacion.Exigir(dto.FechaInicio.Kind == DateTimeKind.Unspecified && dto.FechaInicio.Ticks % TimeSpan.TicksPerMinute == 0,
            "La fecha debe ser local, sin zona ni segundos adicionales.", "400");
        _context.ChangeTracker.Clear();
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var cliente = await _context.Usuarios.FromSqlInterpolated($"SELECT * FROM \"Usuarios\" WHERE \"Id\" = {usuario} FOR UPDATE")
                .AsNoTracking().SingleOrDefaultAsync(ct);
            CitaOperacion.Exigir(cliente != null, "Cliente no encontrado.", "404");
            var hash = CitaOperacion.Hash(dto);
            var existente = await _context.Citas.AsNoTracking().SingleOrDefaultAsync(x => x.IdUsuario == usuario && x.IdempotencyKey == dto.IdempotencyKey, ct);
            if (existente != null)
            {
                CitaOperacion.Exigir(existente.RequestHash == hash, "La clave de reserva ya fue utilizada con otros datos.");
                await tx.CommitAsync(ct); return existente;
            }
            var (servicio, zona) = await ServicioAsync(dto.ProductoUuid, ct);
            CitaOperacion.Exigir(dto.ZonaHoraria == zona, "La zona del comercio cambió. Actualiza sus horarios antes de reservar.");
            await ValidarEspacioAsync(servicio, dto.FechaInicio, null, zona, ct);
            var cita = new Cita { IdUsuario = usuario, IdComercio = servicio.IdComercio, IdProductoServicio = servicio.Id,
                NombrePersona = dto.NombrePersona.Trim(), NotasCliente = dto.Notas?.Trim(), FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaInicio.AddMinutes(servicio.DuracionMinutos!.Value), ZonaHoraria = zona, IdempotencyKey = dto.IdempotencyKey, RequestHash = hash };
            _context.Citas.Add(cita); await _context.SaveChangesAsync(ct);
            await ReservarEspacioAsync(cita, ct); Historial(cita, usuario, "Reserva", cita.Estado, cita.FechaInicio, null);
            await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct); return cita;
        }
        catch { await tx.RollbackAsync(CancellationToken.None); _context.ChangeTracker.Clear(); throw; }
    }
    public async Task<Cita> OperarAsync(Guid uuid, long actor, long? comercio, long version, EstadoCita? estado,
        string? atiende, string? motivo, DateTime? inicio, CancellationToken ct = default, string? zonaHoraria = null)
    {
        CitaOperacion.Exigir(version >= 0 && atiende?.Length is not > 100 && motivo?.Length is not > 500, "Datos de cita inválidos.", "400");
        _context.ChangeTracker.Clear();
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var lectura = await _context.Citas.AsNoTracking().SingleOrDefaultAsync(x => x.Uuid == uuid &&
                (comercio.HasValue ? x.IdComercio == comercio : x.IdUsuario == actor), ct);
            if (lectura == null) throw new CitaOperacionException("404", "Cita no encontrada.");
            await _context.Comercios.FromSqlInterpolated($"SELECT * FROM \"Comercios\" WHERE \"Id\" = {lectura.IdComercio} FOR UPDATE").AsNoTracking().SingleAsync(ct);
            var cita = await _context.Citas.FromSqlInterpolated($"SELECT * FROM citas WHERE \"Id\" = {lectura.Id} FOR UPDATE").AsTracking().SingleAsync(ct);
            CitaOperacion.Exigir(cita.Version == version, "La cita cambió. Actualiza la agenda antes de continuar.");
            var anterior = cita.Estado; var fechaAnterior = cita.FechaInicio; var ahora = CitaOperacion.Ahora(_reloj, cita.ZonaHoraria);
            if (!comercio.HasValue)
                CitaOperacion.Exigir(cita.Estado is EstadoCita.Pendiente or EstadoCita.Confirmada && cita.FechaInicio > ahora,
                    "Esta cita ya no permite cambios del cliente.");
            if (inicio.HasValue)
            {
                CitaOperacion.Exigir(cita.Estado is EstadoCita.Pendiente or EstadoCita.Confirmada, "Esta cita ya no se puede reprogramar.");
                var producto = await _context.ProductosServicios.AsNoTracking().SingleAsync(x => x.Id == cita.IdProductoServicio, ct);
                var (servicio, zona) = await ServicioAsync(producto.Uuid, ct);
                CitaOperacion.Exigir(zonaHoraria == zona && zona == cita.ZonaHoraria, "La zona cambió. Actualiza la cita antes de reprogramar.");
                await ValidarEspacioAsync(servicio, inicio.Value, cita.Id, zona, ct);
                await LiberarAsync(cita.Id, ct);
                cita.FechaInicio = inicio.Value; cita.FechaFin = inicio.Value.AddMinutes(servicio.DuracionMinutos!.Value);
                cita.Estado = EstadoCita.Pendiente; await ReservarEspacioAsync(cita, ct);
            }
            else
            {
                CitaOperacion.Exigir(estado.HasValue && Enum.IsDefined(estado.Value), "Estado de cita inválido.", "400");
                CitaOperacion.Exigir(CitaOperacion.Acciones(cita.Estado).Contains(estado!.Value) ||
                    (comercio.HasValue && cita.Estado == estado && CitaOperacion.Acciones(cita.Estado).Count > 0), "La transición de la cita no está permitida.");
                if (estado == EstadoCita.Cancelada)
                {
                    CitaOperacion.Exigir(!string.IsNullOrWhiteSpace(motivo), "Indica el motivo de cancelación.", "400");
                    cita.MotivoCancelacion = motivo!.Trim(); await LiberarAsync(cita.Id, ct);
                }
                if (estado is EstadoCita.EnAtencion or EstadoCita.NoAsistio)
                    CitaOperacion.Exigir(ahora >= cita.FechaInicio, "Todavía no comienza el horario de la cita.");
                if (estado == EstadoCita.Completada)
                    CitaOperacion.Exigir(ahora >= cita.FechaFin, "Todavía no termina el horario reservado.");
                cita.Estado = estado!.Value;
                if (comercio.HasValue) cita.NombreAtiende = atiende?.Trim();
            }
            cita.Version++; cita.FechaActualizacion = _reloj.GetUtcNow().UtcDateTime;
            Historial(cita, actor, inicio.HasValue ? "Reprogramacion" : "Estado", anterior, fechaAnterior, motivo);
            await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct); return cita;
        }
        catch { await tx.RollbackAsync(CancellationToken.None); _context.ChangeTracker.Clear(); throw; }
    }
    private async Task ValidarEspacioAsync(ProductosServicios servicio, DateTime inicio, long? excluir, string zona, CancellationToken ct)
    {
        CitaOperacion.Fecha(inicio, CitaOperacion.Ahora(_reloj, zona)); var fecha = DateOnly.FromDateTime(inicio);
        var horario = await _context.HorarioComercio.AsNoTracking().SingleOrDefaultAsync(x => x.ComercioId == servicio.IdComercio && x.Dia == fecha.DayOfWeek, ct);
        CitaOperacion.Exigir(CitaOperacion.Espacios(servicio, horario, fecha).Contains(inicio), "El horario seleccionado ya no está disponible.");
        var fin = inicio.AddMinutes(servicio.DuracionMinutos!.Value);
        CitaOperacion.Exigir(CitaOperacion.IntervaloValido(inicio, fin, zona), "Ese intervalo es ambiguo o cambia su duración por horario estacional. Selecciona otro.", "400");
        CitaOperacion.Exigir(!await _context.Citas.AnyAsync(x => x.IdComercio == servicio.IdComercio && x.Id != excluir &&
            x.Estado != EstadoCita.Cancelada && x.FechaInicio < fin && x.FechaFin > inicio, ct), "El horario seleccionado ya no está disponible.");
    }
    private Task LiberarAsync(long cita, CancellationToken ct) => _context.HorariosCitaServicio.Where(x => x.IdCita == cita)
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.IdCita, (long?)null).SetProperty(x => x.Disponible, true), ct);
    private async Task ReservarEspacioAsync(Cita cita, CancellationToken ct)
    {
        var fecha = DateOnly.FromDateTime(cita.FechaInicio); var hora = cita.FechaInicio.TimeOfDay;
        var espacio = await _context.HorariosCitaServicio.AsTracking().SingleOrDefaultAsync(x => x.IdProductoServicio == cita.IdProductoServicio && x.Fecha == fecha && x.HoraInicio == hora, ct);
        if (espacio == null) { espacio = new() { IdProductoServicio = cita.IdProductoServicio, IdComercio = cita.IdComercio, Fecha = fecha, HoraInicio = hora }; _context.HorariosCitaServicio.Add(espacio); }
        // Citas are authoritative; this also repairs stale links from the previous writer.
        espacio.IdCita = cita.Id; espacio.Disponible = false; espacio.HoraFin = cita.FechaFin.TimeOfDay;
    }
    private void Historial(Cita cita, long actor, string evento, EstadoCita anterior, DateTime inicio, string? comentario) =>
        _context.CitaHistorial.Add(new() { IdCita = cita.Id, IdUsuario = actor, Evento = evento, EstadoAnterior = anterior,
            EstadoNuevo = cita.Estado, InicioAnterior = inicio, InicioNuevo = cita.FechaInicio,
            Comentario = comentario?.Trim(), Fecha = _reloj.GetUtcNow().UtcDateTime });
}
