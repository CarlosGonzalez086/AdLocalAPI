using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;

namespace AdLocalAPI.Services
{
    public class CotizacionService : ICotizacionService
    {
        private readonly ICotizacionRepository _repository;

        public CotizacionService(ICotizacionRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<object>> CrearCotizacionAsync(long idUsuario, CrearCotizacionDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
            {
                return ApiResponse<object>.Error("400", "Datos de cotización inválidos.");
            }

            if (string.IsNullOrWhiteSpace(dto.Solicitud))
            {
                return ApiResponse<object>.Error("400", "Describe lo que necesitas cotizar.");
            }

            var servicio = await _repository.ObtenerServicioParaCotizarAsync(dto.ProductoUuid, cancellationToken);
            if (servicio == null)
            {
                return ApiResponse<object>.Error("404", "Servicio no encontrado o no disponible para cotización.");
            }

            var cotizacion = new Cotizacion
            {
                IdUsuario = idUsuario,
                IdComercio = servicio.IdComercio,
                IdProductoServicio = servicio.Id,
                Solicitud = dto.Solicitud.Trim(),
                Estado = EstadoCotizacion.Pendiente,
                FechaCreacion = DateTime.UtcNow
            };

            await _repository.CrearAsync(cotizacion, cancellationToken);

            return ApiResponse<object>.Success(new { cotizacion.Uuid }, "Cotización enviada.");
        }

        public async Task<ApiResponse<List<CotizacionItemDto>>> ObtenerMiasAsync(long idUsuario, CancellationToken cancellationToken = default)
        {
            var items = await _repository.ObtenerMiasAsync(idUsuario, cancellationToken);
            return ApiResponse<List<CotizacionItemDto>>.Success(items);
        }

        public async Task<ApiResponse<object>> CancelarCotizacionAsync(long idUsuario, Guid uuid, CancellationToken cancellationToken = default)
        {
            var cotizacion = await _repository.ObtenerPorUuidYUsuarioAsync(uuid, idUsuario, cancellationToken);
            if (cotizacion == null)
            {
                return ApiResponse<object>.Error("404", "Cotización no encontrada.");
            }

            if (cotizacion.Estado is EstadoCotizacion.Aceptada or EstadoCotizacion.Cancelada)
            {
                return ApiResponse<object>.Error("409", "Ya no se puede cancelar la cotización.");
            }

            cotizacion.Estado = EstadoCotizacion.Cancelada;
            cotizacion.FechaActualizacion = DateTime.UtcNow;
            await _repository.ActualizarAsync(cotizacion, cancellationToken);

            return ApiResponse<object>.Success(new { cotizacion.Uuid }, "Cotización cancelada.");
        }
    }
}
