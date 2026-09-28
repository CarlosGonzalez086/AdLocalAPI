using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;

namespace AdLocalAPI.Services
{
    public partial class CitaService : ICitaService
    {
        private readonly ICitaRepository _citaRepository;
        private readonly IProductosServiciosRepository _productoServicioRepository;
        private readonly IHorarioComercioRepository _horarioComercioRepository;
        private readonly IHorarioCitaServicioRepository _horarioCitaRepository;
        private readonly IComercioRepository _comercioRepository;
        private readonly JwtContext _jwt;

        public CitaService(
            ICitaRepository citaRepository,
            IProductosServiciosRepository productoServicioRepository,
            IHorarioComercioRepository horarioComercioRepository,
            IHorarioCitaServicioRepository horarioCitaRepository,
            IComercioRepository comercioRepository,
            JwtContext jwt
        )
        {
            _citaRepository = citaRepository;
            _productoServicioRepository = productoServicioRepository;
            _horarioComercioRepository = horarioComercioRepository;
            _horarioCitaRepository = horarioCitaRepository;
            _comercioRepository = comercioRepository;
            _jwt = jwt;
        }

        // ============================================================
        // HORARIOS DISPONIBLES
        // ============================================================

        public async Task<ApiResponse<List<string>>> HorariosAsync(Guid productoUuid, DateOnly fecha)
        {
            var servicio = await _productoServicioRepository.ObtenerReservablePorUuidAsync(productoUuid);
            if (servicio == null)
            {
                return ApiResponse<List<string>>.Error("404", "Servicio no encontrado.");
            }

            var horario = await _horarioComercioRepository.ObtenerAsync(servicio.IdComercio, fecha.DayOfWeek);
            if (horario == null || !horario.Abierto || !horario.HoraApertura.HasValue || !horario.HoraCierre.HasValue)
            {
                return ApiResponse<List<string>>.Success(new List<string>());
            }

            var duracion = servicio.DuracionMinutos ?? 30;
            var inicioDia = fecha.ToDateTime(TimeOnly.FromTimeSpan(horario.HoraApertura.Value));
            var finDia = fecha.ToDateTime(TimeOnly.FromTimeSpan(horario.HoraCierre.Value));

            var ocupadas = await _citaRepository.ObtenerOcupadasAsync(servicio.IdComercio, inicioDia, finDia);
            var existentes = await _horarioCitaRepository.ObtenerPorServicioFechaAsync(servicio.Id, fecha);

            var iniciosValidos = new HashSet<TimeSpan>();
            for (var hora = inicioDia; hora.AddMinutes(duracion) <= finDia; hora = hora.AddMinutes(duracion))
            {
                var fin = hora.AddMinutes(duracion);
                iniciosValidos.Add(hora.TimeOfDay);

                var yaExiste = existentes.Any(x => x.HoraInicio == hora.TimeOfDay);
                if (!yaExiste)
                {
                    _horarioCitaRepository.Agregar(new HorarioCitaServicio
                    {
                        IdProductoServicio = servicio.Id,
                        IdComercio = servicio.IdComercio,
                        Fecha = fecha,
                        HoraInicio = hora.TimeOfDay,
                        HoraFin = fin.TimeOfDay,
                        Disponible = true
                    });
                }
            }

            var obsoletos = existentes
                .Where(x => x.IdCita == null && !iniciosValidos.Contains(x.HoraInicio))
                .ToList();

            if (obsoletos.Count > 0)
            {
                _horarioCitaRepository.EliminarRango(obsoletos);
            }

            await _horarioCitaRepository.GuardarCambiosAsync();

            var espacios = await _horarioCitaRepository.ObtenerDisponiblesAsync(servicio.Id, fecha);
            var ahora = DateTime.Now;

            var disponibles = espacios
                .Where(x =>
                {
                    var inicio = fecha.ToDateTime(TimeOnly.FromTimeSpan(x.HoraInicio));
                    var fin = fecha.ToDateTime(TimeOnly.FromTimeSpan(x.HoraFin));
                    var ocupado = ocupadas.Any(c => c.FechaInicio < fin && c.FechaFin > inicio);
                    return inicio > ahora && !ocupado;
                })
                .Select(x => x.HoraInicio.ToString(@"hh\:mm"))
                .ToList();

            return ApiResponse<List<string>>.Success(disponibles, $"{disponibles.Count} horarios disponibles.");
        }

        // ============================================================
        // MIS CITAS
        // ============================================================

        public async Task<ApiResponse<List<CitaDto>>> MisCitasAsync()
        {
            var datos = await _citaRepository.ObtenerPorUsuarioAsync(_jwt.GetUserId());
            var ahoraLocal = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            var futuras = datos
                .Where(x => x.FechaInicio >= ahoraLocal && x.Estado != EstadoCita.Cancelada)
                .OrderBy(x => x.FechaInicio);

            var anteriores = datos
                .Where(x => x.FechaInicio < ahoraLocal || x.Estado == EstadoCita.Cancelada)
                .OrderByDescending(x => x.FechaInicio);

            var ordenadas = futuras.Concat(anteriores).ToList();

            return ApiResponse<List<CitaDto>>.Success(ordenadas);
        }

        // ============================================================
        // AGENDA DEL COMERCIO
        // ============================================================

        public async Task<ApiResponse<List<CitaDto>>> AgendaAsync(long comercioId, DateOnly? fecha)
        {
            var puedeAdministrar = await PuedeAdministrarAsync(comercioId);
            if (!puedeAdministrar)
            {
                return ApiResponse<List<CitaDto>>.Error("403", "No tienes acceso a este comercio.");
            }

            var datos = await _citaRepository.ObtenerAgendaAsync(comercioId, fecha);
            return ApiResponse<List<CitaDto>>.Success(datos);
        }

        // ============================================================
        // PRIVADOS
        // ============================================================

        private async Task<bool> PuedeAdministrarAsync(long comercioId)
        {
            var usuarioId = _jwt.GetUserId();
            return await _comercioRepository.PuedeAdministrarAsync(comercioId, usuarioId);
        }

        private async Task<ApiResponse<CitaDto>> ObtenerRespuestaDtoAsync(long citaId)
        {
            var dto = await _citaRepository.ObtenerDtoAsync(citaId);
            if (dto == null)
            {
                return ApiResponse<CitaDto>.Error("500", "No fue posible obtener la información de la cita.");
            }

            return ApiResponse<CitaDto>.Success(dto);
        }
    }
}