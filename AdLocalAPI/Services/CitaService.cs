using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Services
{
    public class CitaService
    {
        private readonly AppDbContext _context;
        private readonly JwtContext _jwt;
        public CitaService(AppDbContext context, JwtContext jwt) { _context = context; _jwt = jwt; }

        public async Task<ApiResponse<List<string>>> HorariosAsync(Guid productoUuid, DateOnly fecha)
        {
            var servicio = await _context.ProductosServicios.AsNoTracking().FirstOrDefaultAsync(x => x.Uuid == productoUuid && x.Activo && x.Visible && x.Disponible && x.Modalidad == ModalidadProductoServicio.Reservacion);
            if (servicio == null) return ApiResponse<List<string>>.Error("404", "Servicio no encontrado.");
            var horario = await _context.HorarioComercio.AsNoTracking().FirstOrDefaultAsync(x => x.ComercioId == servicio.IdComercio && x.Dia == fecha.DayOfWeek);
            if (horario == null || !horario.Abierto || !horario.HoraApertura.HasValue || !horario.HoraCierre.HasValue)
                return ApiResponse<List<string>>.Success(new());

            var duracion = servicio.DuracionMinutos ?? 30;
            var inicioDia = fecha.ToDateTime(TimeOnly.FromTimeSpan(horario.HoraApertura.Value));
            var finDia = fecha.ToDateTime(TimeOnly.FromTimeSpan(horario.HoraCierre.Value));
            var ocupadas = await _context.Citas.AsNoTracking().Where(x => x.IdComercio == servicio.IdComercio && x.Estado != EstadoCita.Cancelada && x.FechaInicio < finDia && x.FechaFin > inicioDia).Select(x => new { x.FechaInicio, x.FechaFin }).ToListAsync();
            var ahora = DateTime.Now;
            var existentes = await _context.HorariosCitaServicio.Where(x => x.IdProductoServicio == servicio.Id && x.Fecha == fecha).ToListAsync();
            var iniciosValidos = new HashSet<TimeSpan>();
            for (var hora = inicioDia; hora.AddMinutes(duracion) <= finDia; hora = hora.AddMinutes(duracion))
            {
                var fin = hora.AddMinutes(duracion);
                iniciosValidos.Add(hora.TimeOfDay);
                if (!existentes.Any(x => x.HoraInicio == hora.TimeOfDay))
                    _context.HorariosCitaServicio.Add(new HorarioCitaServicio { IdProductoServicio = servicio.Id, IdComercio = servicio.IdComercio, Fecha = fecha, HoraInicio = hora.TimeOfDay, HoraFin = fin.TimeOfDay });
            }
            var obsoletos = existentes.Where(x => x.IdCita == null && !iniciosValidos.Contains(x.HoraInicio)).ToList();
            if (obsoletos.Count > 0) _context.HorariosCitaServicio.RemoveRange(obsoletos);
            await _context.SaveChangesAsync();

            var espacios = await _context.HorariosCitaServicio.AsNoTracking().Where(x => x.IdProductoServicio == servicio.Id && x.Fecha == fecha && x.Disponible && x.IdCita == null).OrderBy(x => x.HoraInicio).ToListAsync();
            var disponibles = espacios.Where(x =>
            {
                var inicio = fecha.ToDateTime(TimeOnly.FromTimeSpan(x.HoraInicio));
                var fin = fecha.ToDateTime(TimeOnly.FromTimeSpan(x.HoraFin));
                return inicio > ahora && !ocupadas.Any(c => c.FechaInicio < fin && c.FechaFin > inicio);
            }).Select(x => x.HoraInicio.ToString(@"hh\:mm")).ToList();
            return ApiResponse<List<string>>.Success(disponibles, $"{disponibles.Count} horarios disponibles.");
        }

        public async Task<ApiResponse<CitaDto>> CrearAsync(CrearCitaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NombrePersona)) return ApiResponse<CitaDto>.Error("400", "Indica el nombre de la persona que recibirá la atención.");
            var fechaInicioLocal = DateTime.SpecifyKind(dto.FechaInicio, DateTimeKind.Unspecified);
            var servicio = await _context.ProductosServicios.FirstOrDefaultAsync(x => x.Uuid == dto.ProductoUuid && x.Activo && x.Visible && x.Disponible && x.Modalidad == ModalidadProductoServicio.Reservacion);
            if (servicio == null) return ApiResponse<CitaDto>.Error("404", "Servicio no encontrado.");
            var fecha = DateOnly.FromDateTime(fechaInicioLocal);
            var horarios = await HorariosAsync(dto.ProductoUuid, fecha);
            if (horarios.Codigo != "200" || !horarios.Respuesta.Contains(fechaInicioLocal.ToString("HH:mm"))) return ApiResponse<CitaDto>.Error("409", "El horario seleccionado ya no está disponible.");
            var espacio = await _context.HorariosCitaServicio.FirstOrDefaultAsync(x => x.IdProductoServicio == servicio.Id && x.Fecha == fecha && x.HoraInicio == fechaInicioLocal.TimeOfDay && x.Disponible && x.IdCita == null);
            if (espacio == null) return ApiResponse<CitaDto>.Error("409", "El horario seleccionado ya no está disponible.");
            var cita = new Cita { IdUsuario = _jwt.GetUserId(), IdComercio = servicio.IdComercio, IdProductoServicio = servicio.Id, NombrePersona = dto.NombrePersona.Trim(), NotasCliente = dto.Notas?.Trim(), FechaInicio = fechaInicioLocal, FechaFin = fechaInicioLocal.AddMinutes(servicio.DuracionMinutos ?? 30) };
            _context.Citas.Add(cita);
            await _context.SaveChangesAsync();
            espacio.Disponible = false;
            espacio.IdCita = cita.Id;
            await _context.SaveChangesAsync();
            return await ObtenerDtoAsync(cita.Id);
        }

        public async Task<ApiResponse<List<CitaDto>>> MisCitasAsync()
        {
            var datos = await Consulta().Where(x => x.c.IdUsuario == _jwt.GetUserId()).ToListAsync();
            var ahoraLocal = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            var ordenadas = datos
                .Where(x => x.c.FechaInicio >= ahoraLocal && x.c.Estado != EstadoCita.Cancelada)
                .OrderBy(x => x.c.FechaInicio)
                .Concat(datos
                    .Where(x => x.c.FechaInicio < ahoraLocal || x.c.Estado == EstadoCita.Cancelada)
                    .OrderByDescending(x => x.c.FechaInicio));
            return ApiResponse<List<CitaDto>>.Success(ordenadas.Select(x => Map(x.c, x.com.Nombre, x.s.Nombre, x.u.Nombre, x.u.Telefono, x.s.Uuid)).ToList());
        }

        public async Task<ApiResponse<CitaDto>> CancelarClienteAsync(Guid uuid, string? motivo)
        {
            var cita = await _context.Citas.FirstOrDefaultAsync(x => x.Uuid == uuid && x.IdUsuario == _jwt.GetUserId());
            if (cita == null) return ApiResponse<CitaDto>.Error("404", "Cita no encontrada.");
            if (cita.Estado is EstadoCita.Completada or EstadoCita.Cancelada or EstadoCita.NoAsistio)
                return ApiResponse<CitaDto>.Error("409", "Esta cita ya no se puede cancelar.");
            cita.Estado = EstadoCita.Cancelada;
            cita.MotivoCancelacion = motivo?.Trim();
            cita.FechaActualizacion = DateTime.UtcNow;
            var espacio = await _context.HorariosCitaServicio.FirstOrDefaultAsync(x => x.IdCita == cita.Id);
            if (espacio != null) { espacio.IdCita = null; espacio.Disponible = true; }
            await _context.SaveChangesAsync();
            return await ObtenerDtoAsync(cita.Id);
        }

        public async Task<ApiResponse<CitaDto>> ReprogramarClienteAsync(Guid uuid, ReprogramarCitaDto dto)
        {
            var cita = await _context.Citas.FirstOrDefaultAsync(x => x.Uuid == uuid && x.IdUsuario == _jwt.GetUserId());
            if (cita == null) return ApiResponse<CitaDto>.Error("404", "Cita no encontrada.");
            if (cita.Estado is not (EstadoCita.Pendiente or EstadoCita.Confirmada))
                return ApiResponse<CitaDto>.Error("409", "Esta cita ya no se puede reprogramar.");
            var servicio = await _context.ProductosServicios.FirstAsync(x => x.Id == cita.IdProductoServicio);
            var inicio = DateTime.SpecifyKind(dto.FechaInicio, DateTimeKind.Unspecified);
            var fecha = DateOnly.FromDateTime(inicio);
            var horarios = await HorariosAsync(servicio.Uuid, fecha);
            if (horarios.Codigo != "200" || !horarios.Respuesta.Contains(inicio.ToString("HH:mm")))
                return ApiResponse<CitaDto>.Error("409", "El horario seleccionado ya no está disponible.");
            var nuevo = await _context.HorariosCitaServicio.FirstOrDefaultAsync(x => x.IdProductoServicio == servicio.Id && x.Fecha == fecha && x.HoraInicio == inicio.TimeOfDay && x.Disponible && x.IdCita == null);
            if (nuevo == null) return ApiResponse<CitaDto>.Error("409", "El horario seleccionado ya no está disponible.");
            var anterior = await _context.HorariosCitaServicio.FirstOrDefaultAsync(x => x.IdCita == cita.Id);
            if (anterior != null) { anterior.IdCita = null; anterior.Disponible = true; }
            nuevo.IdCita = cita.Id; nuevo.Disponible = false;
            cita.FechaInicio = inicio; cita.FechaFin = inicio.AddMinutes(servicio.DuracionMinutos ?? 30); cita.Estado = EstadoCita.Pendiente; cita.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return await ObtenerDtoAsync(cita.Id);
        }

        public async Task<ApiResponse<List<CitaDto>>> AgendaAsync(long comercioId, DateOnly? fecha)
        {
            if (!await PuedeAdministrarAsync(comercioId)) return ApiResponse<List<CitaDto>>.Error("403", "No tienes acceso a este comercio.");
            var query = Consulta().Where(x => x.c.IdComercio == comercioId);
            if (fecha.HasValue) { var desde = fecha.Value.ToDateTime(TimeOnly.MinValue); var hasta = desde.AddDays(1); query = query.Where(x => x.c.FechaInicio >= desde && x.c.FechaInicio < hasta); }
            var datos = await query.OrderBy(x => x.c.FechaInicio).ToListAsync();
            return ApiResponse<List<CitaDto>>.Success(datos.Select(x => Map(x.c, x.com.Nombre, x.s.Nombre, x.u.Nombre, x.u.Telefono, x.s.Uuid)).ToList());
        }

        public async Task<ApiResponse<CitaDto>> ActualizarAsync(long comercioId, Guid uuid, ActualizarCitaComercioDto dto)
        {
            if (!await PuedeAdministrarAsync(comercioId)) return ApiResponse<CitaDto>.Error("403", "No tienes acceso a este comercio.");
            var cita = await _context.Citas.FirstOrDefaultAsync(x => x.Uuid == uuid && x.IdComercio == comercioId);
            if (cita == null) return ApiResponse<CitaDto>.Error("404", "Cita no encontrada.");
            cita.Estado = dto.Estado; cita.NombreAtiende = dto.NombreAtiende?.Trim(); cita.MotivoCancelacion = dto.Motivo?.Trim(); cita.FechaActualizacion = DateTime.UtcNow;
            if (dto.Estado == EstadoCita.Cancelada)
            {
                var espacio = await _context.HorariosCitaServicio.FirstOrDefaultAsync(x => x.IdCita == cita.Id);
                if (espacio != null) { espacio.IdCita = null; espacio.Disponible = true; }
            }
            await _context.SaveChangesAsync();
            return await ObtenerDtoAsync(cita.Id);
        }

        private IQueryable<CitaConsulta> Consulta() => from c in _context.Citas.AsNoTracking() join com in _context.Comercios.AsNoTracking() on c.IdComercio equals com.Id join s in _context.ProductosServicios.AsNoTracking() on c.IdProductoServicio equals s.Id join u in _context.Usuarios.AsNoTracking() on c.IdUsuario equals u.Id select new CitaConsulta { c = c, com = com, s = s, u = u };
        private static CitaDto Map(Cita c, string comercio, string servicio, string cliente, string? telefono, Guid productoUuid) => new() { Uuid = c.Uuid, ProductoUuid = productoUuid, Comercio = comercio, Servicio = servicio, Cliente = cliente, TelefonoCliente = telefono, NombrePersona = c.NombrePersona, NotasCliente = c.NotasCliente, NombreAtiende = c.NombreAtiende, FechaInicio = c.FechaInicio, FechaFin = c.FechaFin, Estado = c.Estado, MotivoCancelacion = c.MotivoCancelacion };
        private async Task<ApiResponse<CitaDto>> ObtenerDtoAsync(long id) { var x = await Consulta().FirstAsync(x => x.c.Id == id); return ApiResponse<CitaDto>.Success(Map(x.c, x.com.Nombre, x.s.Nombre, x.u.Nombre, x.u.Telefono, x.s.Uuid)); }
        private async Task<bool> PuedeAdministrarAsync(long comercioId) { var uid = _jwt.GetUserId(); return await _context.Comercios.AnyAsync(x => x.Id == comercioId && x.IdUsuario == uid) || await _context.Usuarios.AnyAsync(x => x.Id == uid && x.ComercioId == comercioId); }
        private sealed class CitaConsulta { public Cita c { get; set; } = null!; public Comercio com { get; set; } = null!; public ProductosServicios s { get; set; } = null!; public Usuario u { get; set; } = null!; }
    }
}
