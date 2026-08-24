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
    public class CitaService : ICitaService
    {
        private readonly ICitaRepository _citaRepository;
        private readonly IProductosServiciosRepository _productoServicioRepository;
        private readonly IHorarioComercioRepository _horarioComercioRepository;
        private readonly IHorarioCitaServicioRepository _horarioCitaRepository;
        private readonly ComercioRepository _comercioRepository;
        private readonly JwtContext _jwt;

        public CitaService(
            ICitaRepository citaRepository,
            IProductosServiciosRepository productoServicioRepository,
            IHorarioComercioRepository horarioComercioRepository,
            IHorarioCitaServicioRepository horarioCitaRepository,
            ComercioRepository comercioRepository,
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

        public async Task<ApiResponse<List<string>>> HorariosAsync(
            Guid productoUuid,
            DateOnly fecha
        )
        {
            var servicio =
                await _productoServicioRepository.ObtenerReservablePorUuidAsync(
                    productoUuid
                );

            if (servicio == null)
            {
                return ApiResponse<List<string>>.Error(
                    "404",
                    "Servicio no encontrado."
                );
            }

            var horario =
                await _horarioComercioRepository.ObtenerAsync(
                    servicio.IdComercio,
                    fecha.DayOfWeek
                );

            if (
                horario == null ||
                !horario.Abierto ||
                !horario.HoraApertura.HasValue ||
                !horario.HoraCierre.HasValue
            )
            {
                return ApiResponse<List<string>>.Success(
                    new List<string>()
                );
            }

            var duracion = servicio.DuracionMinutos ?? 30;

            var inicioDia = fecha.ToDateTime(
                TimeOnly.FromTimeSpan(
                    horario.HoraApertura.Value
                )
            );

            var finDia = fecha.ToDateTime(
                TimeOnly.FromTimeSpan(
                    horario.HoraCierre.Value
                )
            );

            var ocupadas =
                await _citaRepository.ObtenerOcupadasAsync(
                    servicio.IdComercio,
                    inicioDia,
                    finDia
                );

            var existentes =
                await _horarioCitaRepository.ObtenerPorServicioFechaAsync(
                    servicio.Id,
                    fecha
                );

            var iniciosValidos = new HashSet<TimeSpan>();

            for (
                var hora = inicioDia;
                hora.AddMinutes(duracion) <= finDia;
                hora = hora.AddMinutes(duracion)
            )
            {
                var fin = hora.AddMinutes(duracion);

                iniciosValidos.Add(
                    hora.TimeOfDay
                );

                var yaExiste = existentes.Any(
                    x =>
                        x.HoraInicio ==
                        hora.TimeOfDay
                );

                if (!yaExiste)
                {
                    _horarioCitaRepository.Agregar(
                        new HorarioCitaServicio
                        {
                            IdProductoServicio =
                                servicio.Id,

                            IdComercio =
                                servicio.IdComercio,

                            Fecha =
                                fecha,

                            HoraInicio =
                                hora.TimeOfDay,

                            HoraFin =
                                fin.TimeOfDay,

                            Disponible =
                                true
                        }
                    );
                }
            }

            var obsoletos =
                existentes
                    .Where(
                        x =>
                            x.IdCita == null &&
                            !iniciosValidos.Contains(
                                x.HoraInicio
                            )
                    )
                    .ToList();

            if (obsoletos.Count > 0)
            {
                _horarioCitaRepository.EliminarRango(
                    obsoletos
                );
            }

            await _horarioCitaRepository.GuardarCambiosAsync();

            var espacios =
                await _horarioCitaRepository.ObtenerDisponiblesAsync(
                    servicio.Id,
                    fecha
                );

            var ahora = DateTime.Now;

            var disponibles =
                espacios
                    .Where(
                        x =>
                        {
                            var inicio =
                                fecha.ToDateTime(
                                    TimeOnly.FromTimeSpan(
                                        x.HoraInicio
                                    )
                                );

                            var fin =
                                fecha.ToDateTime(
                                    TimeOnly.FromTimeSpan(
                                        x.HoraFin
                                    )
                                );

                            var ocupado =
                                ocupadas.Any(
                                    c =>
                                        c.FechaInicio < fin &&
                                        c.FechaFin > inicio
                                );

                            return inicio > ahora &&
                                   !ocupado;
                        }
                    )
                    .Select(
                        x =>
                            x.HoraInicio.ToString(
                                @"hh\:mm"
                            )
                    )
                    .ToList();

            return ApiResponse<List<string>>.Success(
                disponibles,
                $"{disponibles.Count} horarios disponibles."
            );
        }

        // ============================================================
        // CREAR CITA
        // ============================================================

        public async Task<ApiResponse<CitaDto>> CrearAsync(
            CrearCitaDto dto
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    dto.NombrePersona
                )
            )
            {
                return ApiResponse<CitaDto>.Error(
                    "400",
                    "Indica el nombre de la persona que recibirá la atención."
                );
            }

            var fechaInicioLocal =
                DateTime.SpecifyKind(
                    dto.FechaInicio,
                    DateTimeKind.Unspecified
                );

            var servicio =
                await _productoServicioRepository.ObtenerReservablePorUuidAsync(
                    dto.ProductoUuid
                );

            if (servicio == null)
            {
                return ApiResponse<CitaDto>.Error(
                    "404",
                    "Servicio no encontrado."
                );
            }

            var fecha =
                DateOnly.FromDateTime(
                    fechaInicioLocal
                );

            var horarios =
                await HorariosAsync(
                    dto.ProductoUuid,
                    fecha
                );

            if (
                horarios.Codigo != "200" ||
                !horarios.Respuesta.Contains(
                    fechaInicioLocal.ToString(
                        "HH:mm"
                    )
                )
            )
            {
                return ApiResponse<CitaDto>.Error(
                    "409",
                    "El horario seleccionado ya no está disponible."
                );
            }

            var espacio =
                await _horarioCitaRepository.ObtenerDisponibleAsync(
                    servicio.Id,
                    fecha,
                    fechaInicioLocal.TimeOfDay
                );

            if (espacio == null)
            {
                return ApiResponse<CitaDto>.Error(
                    "409",
                    "El horario seleccionado ya no está disponible."
                );
            }

            var cita = new Cita
            {
                IdUsuario =
                    _jwt.GetUserId(),

                IdComercio =
                    servicio.IdComercio,

                IdProductoServicio =
                    servicio.Id,

                NombrePersona =
                    dto.NombrePersona.Trim(),

                NotasCliente =
                    dto.Notas?.Trim(),

                FechaInicio =
                    fechaInicioLocal,

                FechaFin =
                    fechaInicioLocal.AddMinutes(
                        servicio.DuracionMinutos ??
                        30
                    )
            };

            await _citaRepository.CrearAsync(
                cita
            );

            espacio.Disponible = false;
            espacio.IdCita = cita.Id;

            await _horarioCitaRepository.GuardarCambiosAsync();

            return await ObtenerRespuestaDtoAsync(
                cita.Id
            );
        }

        // ============================================================
        // MIS CITAS
        // ============================================================

        public async Task<ApiResponse<List<CitaDto>>> MisCitasAsync()
        {
            var datos =
                await _citaRepository.ObtenerPorUsuarioAsync(
                    _jwt.GetUserId()
                );

            var ahoraLocal =
                DateTime.SpecifyKind(
                    DateTime.Now,
                    DateTimeKind.Unspecified
                );

            var futuras =
                datos
                    .Where(
                        x =>
                            x.FechaInicio >= ahoraLocal &&
                            x.Estado != EstadoCita.Cancelada
                    )
                    .OrderBy(
                        x =>
                            x.FechaInicio
                    );

            var anteriores =
                datos
                    .Where(
                        x =>
                            x.FechaInicio < ahoraLocal ||
                            x.Estado == EstadoCita.Cancelada
                    )
                    .OrderByDescending(
                        x =>
                            x.FechaInicio
                    );

            var ordenadas =
                futuras
                    .Concat(anteriores)
                    .ToList();

            return ApiResponse<List<CitaDto>>.Success(
                ordenadas
            );
        }

        // ============================================================
        // CANCELAR CITA DESDE CLIENTE
        // ============================================================

        public async Task<ApiResponse<CitaDto>> CancelarClienteAsync(
            Guid uuid,
            string? motivo
        )
        {
            var cita =
                await _citaRepository.ObtenerPorUuidClienteAsync(
                    uuid,
                    _jwt.GetUserId()
                );

            if (cita == null)
            {
                return ApiResponse<CitaDto>.Error(
                    "404",
                    "Cita no encontrada."
                );
            }

            if (
                cita.Estado is
                    EstadoCita.Completada or
                    EstadoCita.Cancelada or
                    EstadoCita.NoAsistio
            )
            {
                return ApiResponse<CitaDto>.Error(
                    "409",
                    "Esta cita ya no se puede cancelar."
                );
            }

            cita.Estado =
                EstadoCita.Cancelada;

            cita.MotivoCancelacion =
                motivo?.Trim();

            cita.FechaActualizacion =
                DateTime.UtcNow;

            var espacio =
                await _horarioCitaRepository.ObtenerPorCitaAsync(
                    cita.Id
                );

            if (espacio != null)
            {
                espacio.IdCita = null;
                espacio.Disponible = true;
            }

            await _citaRepository.GuardarCambiosAsync(cita);

            return await ObtenerRespuestaDtoAsync(
                cita.Id
            );
        }

        // ============================================================
        // REPROGRAMAR CITA DESDE CLIENTE
        // ============================================================

        public async Task<ApiResponse<CitaDto>> ReprogramarClienteAsync(
            Guid uuid,
            ReprogramarCitaDto dto
        )
        {
            var cita =
                await _citaRepository.ObtenerPorUuidClienteAsync(
                    uuid,
                    _jwt.GetUserId()
                );

            if (cita == null)
            {
                return ApiResponse<CitaDto>.Error(
                    "404",
                    "Cita no encontrada."
                );
            }

            if (
                cita.Estado is not (
                    EstadoCita.Pendiente or
                    EstadoCita.Confirmada
                )
            )
            {
                return ApiResponse<CitaDto>.Error(
                    "409",
                    "Esta cita ya no se puede reprogramar."
                );
            }

            var servicio =
                await _productoServicioRepository.ObtenerPorIdAsync(
                    cita.IdProductoServicio
                );

            if (servicio == null)
            {
                return ApiResponse<CitaDto>.Error(
                    "404",
                    "Servicio no encontrado."
                );
            }

            var inicio =
                DateTime.SpecifyKind(
                    dto.FechaInicio,
                    DateTimeKind.Unspecified
                );

            var fecha =
                DateOnly.FromDateTime(
                    inicio
                );

            var horarios =
                await HorariosAsync(
                    servicio.Uuid,
                    fecha
                );

            if (
                horarios.Codigo != "200" ||
                !horarios.Respuesta.Contains(
                    inicio.ToString(
                        "HH:mm"
                    )
                )
            )
            {
                return ApiResponse<CitaDto>.Error(
                    "409",
                    "El horario seleccionado ya no está disponible."
                );
            }

            var nuevo =
                await _horarioCitaRepository.ObtenerDisponibleAsync(
                    servicio.Id,
                    fecha,
                    inicio.TimeOfDay
                );

            if (nuevo == null)
            {
                return ApiResponse<CitaDto>.Error(
                    "409",
                    "El horario seleccionado ya no está disponible."
                );
            }

            var anterior =
                await _horarioCitaRepository.ObtenerPorCitaAsync(
                    cita.Id
                );

            if (anterior != null)
            {
                anterior.IdCita = null;
                anterior.Disponible = true;
            }

            nuevo.IdCita =
                cita.Id;

            nuevo.Disponible =
                false;

            cita.FechaInicio =
                inicio;

            cita.FechaFin =
                inicio.AddMinutes(
                    servicio.DuracionMinutos ??
                    30
                );

            cita.Estado =
                EstadoCita.Pendiente;

            cita.FechaActualizacion =
                DateTime.UtcNow;

            await _citaRepository.GuardarCambiosAsync(cita);

            return await ObtenerRespuestaDtoAsync(
                cita.Id
            );
        }

        // ============================================================
        // AGENDA DEL COMERCIO
        // ============================================================

        public async Task<ApiResponse<List<CitaDto>>> AgendaAsync(
            long comercioId,
            DateOnly? fecha
        )
        {
            var puedeAdministrar =
                await PuedeAdministrarAsync(
                    comercioId
                );

            if (!puedeAdministrar)
            {
                return ApiResponse<List<CitaDto>>.Error(
                    "403",
                    "No tienes acceso a este comercio."
                );
            }

            var datos =
                await _citaRepository.ObtenerAgendaAsync(
                    comercioId,
                    fecha
                );

            return ApiResponse<List<CitaDto>>.Success(
                datos
            );
        }

        // ============================================================
        // ACTUALIZAR CITA DESDE COMERCIO
        // ============================================================

        public async Task<ApiResponse<CitaDto>> ActualizarAsync(
            long comercioId,
            Guid uuid,
            ActualizarCitaComercioDto dto
        )
        {
            var puedeAdministrar =
                await PuedeAdministrarAsync(
                    comercioId
                );

            if (!puedeAdministrar)
            {
                return ApiResponse<CitaDto>.Error(
                    "403",
                    "No tienes acceso a este comercio."
                );
            }

            var cita =
                await _citaRepository.ObtenerPorUuidComercioAsync(
                    uuid,
                    comercioId
                );

            if (cita == null)
            {
                return ApiResponse<CitaDto>.Error(
                    "404",
                    "Cita no encontrada."
                );
            }

            cita.Estado =
                dto.Estado;

            cita.NombreAtiende =
                dto.NombreAtiende?.Trim();

            cita.MotivoCancelacion =
                dto.Motivo?.Trim();

            cita.FechaActualizacion =
                DateTime.UtcNow;

            if (
                dto.Estado ==
                EstadoCita.Cancelada
            )
            {
                var espacio =
                    await _horarioCitaRepository.ObtenerPorCitaAsync(
                        cita.Id
                    );

                if (espacio != null)
                {
                    espacio.IdCita = null;
                    espacio.Disponible = true;
                }
            }

            await _citaRepository.GuardarCambiosAsync(cita);

            return await ObtenerRespuestaDtoAsync(
                cita.Id
            );
        }

        // ============================================================
        // PRIVADOS
        // ============================================================

        private async Task<bool> PuedeAdministrarAsync(
            long comercioId
        )
        {
            var usuarioId =
                _jwt.GetUserId();

            return await _comercioRepository.PuedeAdministrarAsync(
                comercioId,
                usuarioId
            );
        }

        private async Task<ApiResponse<CitaDto>> ObtenerRespuestaDtoAsync(
            long citaId
        )
        {
            var dto =
                await _citaRepository.ObtenerDtoAsync(
                    citaId
                );

            if (dto == null)
            {
                return ApiResponse<CitaDto>.Error(
                    "500",
                    "No fue posible obtener la información de la cita."
                );
            }

            return ApiResponse<CitaDto>.Success(
                dto
            );
        }
    }
}