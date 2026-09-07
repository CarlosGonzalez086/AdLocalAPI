using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Services.Interfaces;

namespace AdLocalAPI.Services
{
    public partial class CitaService : ICitaService
    {
        // ============================================================
        // CREAR CITA
        // ============================================================

        public async Task<ApiResponse<CitaDto>> CrearAsync(CrearCitaDto dto, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(dto.NombrePersona))
            {
                return ApiResponse<CitaDto>.Error("400", "Indica el nombre de la persona que recibirá la atención.");
            }

            var fechaInicioLocal = DateTime.SpecifyKind(dto.FechaInicio, DateTimeKind.Unspecified);
            var servicio = await _productoServicioRepository.ObtenerReservablePorUuidAsync(dto.ProductoUuid);
            if (servicio == null)
            {
                return ApiResponse<CitaDto>.Error("404", "Servicio no encontrado.");
            }

            var fecha = DateOnly.FromDateTime(fechaInicioLocal);
            var horarios = await HorariosAsync(dto.ProductoUuid, fecha);
            if (horarios.Codigo != "200" || horarios.Respuesta == null || !horarios.Respuesta.Contains(fechaInicioLocal.ToString("HH:mm")))
            {
                return ApiResponse<CitaDto>.Error("409", "El horario seleccionado ya no está disponible.");
            }

            var espacio = await _horarioCitaRepository.ObtenerDisponibleAsync(
                servicio.Id,
                fecha,
                fechaInicioLocal.TimeOfDay
            );

            if (espacio == null)
            {
                return ApiResponse<CitaDto>.Error("409", "El horario seleccionado ya no está disponible.");
            }

            var cita = new Cita
            {
                IdUsuario = _jwt.GetUserId(),
                IdComercio = servicio.IdComercio,
                IdProductoServicio = servicio.Id,
                NombrePersona = dto.NombrePersona.Trim(),
                NotasCliente = dto.Notas?.Trim(),
                FechaInicio = fechaInicioLocal,
                FechaFin = fechaInicioLocal.AddMinutes(servicio.DuracionMinutos ?? 30)
            };

            await _citaRepository.CrearAsync(cita);

            espacio.Disponible = false;
            espacio.IdCita = cita.Id;

            await _horarioCitaRepository.GuardarCambiosAsync();

            return await ObtenerRespuestaDtoAsync(cita.Id);
        }

        // ============================================================
        // CANCELAR CITA DESDE CLIENTE
        // ============================================================

        public async Task<ApiResponse<CitaDto>> CancelarClienteAsync(Guid uuid, string? motivo)
        {
            var cita = await _citaRepository.ObtenerPorUuidClienteAsync(uuid, _jwt.GetUserId());
            if (cita == null)
            {
                return ApiResponse<CitaDto>.Error("404", "Cita no encontrada.");
            }

            if (cita.Estado is EstadoCita.Completada or EstadoCita.Cancelada or EstadoCita.NoAsistio)
            {
                return ApiResponse<CitaDto>.Error("409", "Esta cita ya no se puede cancelar.");
            }

            cita.Estado = EstadoCita.Cancelada;
            cita.MotivoCancelacion = motivo?.Trim();
            cita.FechaActualizacion = DateTime.UtcNow;

            var espacio = await _horarioCitaRepository.ObtenerPorCitaAsync(cita.Id);
            if (espacio != null)
            {
                espacio.IdCita = null;
                espacio.Disponible = true;
            }

            await _citaRepository.GuardarCambiosAsync(cita);

            return await ObtenerRespuestaDtoAsync(cita.Id);
        }

        // ============================================================
        // REPROGRAMAR CITA DESDE CLIENTE
        // ============================================================

        public async Task<ApiResponse<CitaDto>> ReprogramarClienteAsync(Guid uuid, ReprogramarCitaDto dto)
        {
            var cita = await _citaRepository.ObtenerPorUuidClienteAsync(uuid, _jwt.GetUserId());
            if (cita == null)
            {
                return ApiResponse<CitaDto>.Error("404", "Cita no encontrada.");
            }

            if (cita.Estado is not (EstadoCita.Pendiente or EstadoCita.Confirmada))
            {
                return ApiResponse<CitaDto>.Error("409", "Esta cita ya no se puede reprogramar.");
            }

            var servicio = await _productoServicioRepository.ObtenerPorIdAsync(cita.IdProductoServicio);
            if (servicio == null)
            {
                return ApiResponse<CitaDto>.Error("404", "Servicio no encontrado.");
            }

            var inicio = DateTime.SpecifyKind(dto.FechaInicio, DateTimeKind.Unspecified);
            var fecha = DateOnly.FromDateTime(inicio);

            var horarios = await HorariosAsync(servicio.Uuid, fecha);
            if (horarios.Codigo != "200" || horarios.Respuesta == null || !horarios.Respuesta.Contains(inicio.ToString("HH:mm")))
            {
                return ApiResponse<CitaDto>.Error("409", "El horario seleccionado ya no está disponible.");
            }

            var nuevo = await _horarioCitaRepository.ObtenerDisponibleAsync(servicio.Id, fecha, inicio.TimeOfDay);
            if (nuevo == null)
            {
                return ApiResponse<CitaDto>.Error("409", "El horario seleccionado ya no está disponible.");
            }

            var anterior = await _horarioCitaRepository.ObtenerPorCitaAsync(cita.Id);
            if (anterior != null)
            {
                anterior.IdCita = null;
                anterior.Disponible = true;
            }

            nuevo.IdCita = cita.Id;
            nuevo.Disponible = false;

            cita.FechaInicio = inicio;
            cita.FechaFin = inicio.AddMinutes(servicio.DuracionMinutos ?? 30);
            cita.Estado = EstadoCita.Pendiente;
            cita.FechaActualizacion = DateTime.UtcNow;

            await _citaRepository.GuardarCambiosAsync(cita);

            return await ObtenerRespuestaDtoAsync(cita.Id);
        }

        // ============================================================
        // ACTUALIZAR CITA DESDE COMERCIO
        // ============================================================

        public async Task<ApiResponse<CitaDto>> ActualizarAsync(long comercioId, Guid uuid, ActualizarCitaComercioDto dto)
        {
            var puedeAdministrar = await PuedeAdministrarAsync(comercioId);
            if (!puedeAdministrar)
            {
                return ApiResponse<CitaDto>.Error("403", "No tienes acceso a este comercio.");
            }

            var cita = await _citaRepository.ObtenerPorUuidComercioAsync(uuid, comercioId);
            if (cita == null)
            {
                return ApiResponse<CitaDto>.Error("404", "Cita no encontrada.");
            }

            cita.Estado = dto.Estado;
            cita.NombreAtiende = dto.NombreAtiende?.Trim();
            cita.MotivoCancelacion = dto.Motivo?.Trim();
            cita.FechaActualizacion = DateTime.UtcNow;

            if (dto.Estado == EstadoCita.Cancelada)
            {
                var espacio = await _horarioCitaRepository.ObtenerPorCitaAsync(cita.Id);
                if (espacio != null)
                {
                    espacio.IdCita = null;
                    espacio.Disponible = true;
                }
            }

            await _citaRepository.GuardarCambiosAsync(cita);

            return await ObtenerRespuestaDtoAsync(cita.Id);
        }
    }
}
