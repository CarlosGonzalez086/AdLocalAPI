using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Amazon.S3;
using Amazon.S3.Model;

namespace AdLocalAPI.Services
{
    public class PedidoComercioService : IPedidoComercioService
    {
        private readonly IPedidoComercioRepository _repository;
        private readonly JwtContext _jwt;
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        private readonly INotificacionService _notificaciones;
        private readonly IComisionService _comisiones;

        public PedidoComercioService(
            IPedidoComercioRepository repository,
            JwtContext jwt,
            IAmazonS3 s3,
            IConfiguration configuration,
            INotificacionService notificaciones,
            IComisionService comisiones)
        {
            _repository = repository;
            _jwt = jwt;
            _s3 = s3;
            _bucket = configuration["R2:ComprobantesBucket"] ?? "comprobantes-pago";
            _notificaciones = notificaciones;
            _comisiones = comisiones;
        }

        public async Task<ApiResponse<List<ComercioPedidoSelectorDto>>> ObtenerComerciosAsync() =>
            ApiResponse<List<ComercioPedidoSelectorDto>>.Success(
                await _repository.ObtenerComerciosAsync(_jwt.GetUserId(), _jwt.GetUserRole()));

        public async Task<ApiResponse<PedidosComercioDashboardDto>> ObtenerDashboardAsync(long comercioId)
        {
            if (!await Autorizado(comercioId))
                return ApiResponse<PedidosComercioDashboardDto>.Error("403", "No tienes acceso a este comercio.");

            return ApiResponse<PedidosComercioDashboardDto>.Success(
                await _repository.ObtenerDashboardAsync(comercioId));
        }

        public async Task<ApiResponse<PagedResponse<PedidoComercioListadoDto>>> ObtenerPedidosAsync(
            long comercioId, int page, int pageSize, EstadoPedido? estado)
        {
            if (!await Autorizado(comercioId))
                return ApiResponse<PagedResponse<PedidoComercioListadoDto>>.Error("403", "No tienes acceso a este comercio.");

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            return ApiResponse<PagedResponse<PedidoComercioListadoDto>>.Success(
                await _repository.ObtenerPedidosAsync(comercioId, page, pageSize, estado));
        }

        public async Task<ApiResponse<PedidoComercioDetalleDto>> ObtenerDetalleAsync(
            long comercioId, Guid pedidoUuid)
        {
            if (!await Autorizado(comercioId))
                return ApiResponse<PedidoComercioDetalleDto>.Error("403", "No tienes acceso a este comercio.");

            var pedido = await _repository.ObtenerDetalleAsync(comercioId, pedidoUuid);
            return pedido == null
                ? ApiResponse<PedidoComercioDetalleDto>.Error("404", "Pedido no encontrado.")
                : ApiResponse<PedidoComercioDetalleDto>.Success(pedido);
        }

        public async Task<ApiResponse<PedidoComercioDetalleDto>> CambiarEstadoAsync(
            long comercioId, Guid pedidoUuid, CambiarEstadoPedidoDto dto)
        {
            if (!await Autorizado(comercioId))
                return ApiResponse<PedidoComercioDetalleDto>.Error("403", "No tienes acceso a este comercio.");

            var pedido = await _repository.ObtenerPedidoTrackingAsync(comercioId, pedidoUuid);
            if (pedido == null) return ApiResponse<PedidoComercioDetalleDto>.Error("404", "Pedido no encontrado.");

            var permitidos = PedidoComercioRepository.ObtenerAcciones(pedido.Estado, pedido.TipoEntrega);
            if (!permitidos.Contains(dto.Estado))
                return ApiResponse<PedidoComercioDetalleDto>.Error("409", "La transición de estado no está permitida.");

            if (pedido.EstadoPago == EstadoPagoPedido.Pagado &&
                (dto.Estado == EstadoPedido.Rechazado || dto.Estado == EstadoPedido.Cancelado))
                return ApiResponse<PedidoComercioDetalleDto>.Error(
                    "409", "Un pedido pagado debe reembolsarse antes de rechazarse o cancelarse.");

            var anterior = pedido.Estado;
            pedido.Estado = dto.Estado;
            pedido.FechaActualizacion = DateTime.UtcNow;
            if (dto.Estado == EstadoPedido.Aprobado) pedido.FechaAprobacion = DateTime.UtcNow;
            if (dto.Estado == EstadoPedido.Entregado) pedido.FechaEntrega = DateTime.UtcNow;
            if (dto.Estado == EstadoPedido.Completado) pedido.FechaFinalizacion = DateTime.UtcNow;

            await _repository.GuardarEstadoAsync(pedido, new PedidoHistorialEstado
            {
                Uuid = Guid.NewGuid(),
                IdPedido = pedido.Id,
                EstadoAnterior = anterior,
                EstadoNuevo = dto.Estado,
                IdUsuarioCambio = _jwt.GetUserId(),
                Comentario = string.IsNullOrWhiteSpace(dto.Comentario) ? null : dto.Comentario.Trim(),
                FechaCreacion = DateTime.UtcNow
            });

            await _notificaciones.NotificarClienteAsync(
                pedido,
                TipoNotificacionPedido.EstadoPedidoActualizado,
                "Pedido actualizado",
                $"Tu pedido {pedido.NumeroPedido} ahora está: {ObtenerTextoEstado(dto.Estado)}."
            );

            return await ObtenerDetalleAsync(comercioId, pedidoUuid);
        }

        public async Task<ApiResponse<PedidoComercioDetalleDto>> RevisarPagoAsync(
            long comercioId, Guid pedidoUuid, RevisarPagoPedidoDto dto)
        {
            if (!await Autorizado(comercioId))
                return ApiResponse<PedidoComercioDetalleDto>.Error("403", "No tienes acceso a este comercio.");

            if (dto.EstadoPago != EstadoPagoPedido.Pagado && dto.EstadoPago != EstadoPagoPedido.Rechazado)
                return ApiResponse<PedidoComercioDetalleDto>.Error("400", "Estado de pago no válido.");

            var pedido = await _repository.ObtenerPedidoTrackingAsync(comercioId, pedidoUuid);
            if (pedido == null) return ApiResponse<PedidoComercioDetalleDto>.Error("404", "Pedido no encontrado.");

            ComprobantePago? comprobante = null;
            if (pedido.MetodoPago == MetodoPagoPedido.Transferencia)
            {
                if (pedido.EstadoPago != EstadoPagoPedido.PendienteVerificacion)
                    return ApiResponse<PedidoComercioDetalleDto>.Error("409", "El pago no está pendiente de verificación.");

                comprobante = await _repository.ObtenerComprobanteTrackingAsync(pedido.Id);
                if (comprobante == null)
                    return ApiResponse<PedidoComercioDetalleDto>.Error("404", "No se encontró el comprobante.");

                comprobante.Estatus = (int)dto.EstadoPago;
                comprobante.IdUsuarioValidacion = _jwt.GetUserId();
                comprobante.FechaValidacion = DateTime.UtcNow;
                comprobante.Comentario = string.IsNullOrWhiteSpace(dto.Comentario) ? null : dto.Comentario.Trim();
            }
            else if (dto.EstadoPago == EstadoPagoPedido.Rechazado)
            {
                return ApiResponse<PedidoComercioDetalleDto>.Error("400", "Un pago en efectivo no utiliza comprobante.");
            }

            pedido.EstadoPago = dto.EstadoPago;
            pedido.FechaActualizacion = DateTime.UtcNow;
            await _repository.GuardarPagoAsync(pedido, comprobante);

            if (dto.EstadoPago == EstadoPagoPedido.Pagado)
                await _comisiones.RegistrarVentaAsync(pedido);

            await _notificaciones.NotificarClienteAsync(
                pedido,
                dto.EstadoPago == EstadoPagoPedido.Pagado
                    ? TipoNotificacionPedido.PagoAprobado
                    : TipoNotificacionPedido.PagoRechazado,
                dto.EstadoPago == EstadoPagoPedido.Pagado
                    ? "Pago aprobado"
                    : "Comprobante rechazado",
                dto.EstadoPago == EstadoPagoPedido.Pagado
                    ? $"El pago del pedido {pedido.NumeroPedido} fue aprobado."
                    : $"El comprobante del pedido {pedido.NumeroPedido} fue rechazado. Envía uno nuevo."
            );
            return await ObtenerDetalleAsync(comercioId, pedidoUuid);
        }

        public async Task<ApiResponse<ArchivoComprobanteDto>> ObtenerComprobanteAsync(
            long comercioId, Guid pedidoUuid)
        {
            if (!await Autorizado(comercioId))
                return ApiResponse<ArchivoComprobanteDto>.Error("403", "No tienes acceso a este comercio.");

            var pedido = await _repository.ObtenerPedidoTrackingAsync(comercioId, pedidoUuid);
            if (pedido == null) return ApiResponse<ArchivoComprobanteDto>.Error("404", "Pedido no encontrado.");
            var comprobante = await _repository.ObtenerComprobanteTrackingAsync(pedido.Id);
            if (comprobante == null) return ApiResponse<ArchivoComprobanteDto>.Error("404", "Comprobante no encontrado.");

            try
            {
                using var response = await _s3.GetObjectAsync(new Amazon.S3.Model.GetObjectRequest
                {
                    BucketName = _bucket,
                    Key = comprobante.ArchivoUrl
                });
                await using var memory = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memory);
                var extension = Path.GetExtension(comprobante.ArchivoUrl);

                return ApiResponse<ArchivoComprobanteDto>.Success(new ArchivoComprobanteDto
                {
                    Contenido = memory.ToArray(),
                    ContentType = response.Headers.ContentType ?? "application/octet-stream",
                    Nombre = $"comprobante-{pedido.NumeroPedido}{extension}"
                });
            }
            catch
            {
                return ApiResponse<ArchivoComprobanteDto>.Error("500", "No fue posible abrir el comprobante.");
            }
        }

        private Task<bool> Autorizado(long comercioId) =>
            _repository.PuedeGestionarAsync(_jwt.GetUserId(), _jwt.GetUserRole(), comercioId);

        private static string ObtenerTextoEstado(EstadoPedido estado) => estado switch
        {
            EstadoPedido.Aprobado => "aprobado",
            EstadoPedido.Rechazado => "rechazado",
            EstadoPedido.Preparando => "en preparación",
            EstadoPedido.ListoParaRecoger => "listo para recoger",
            EstadoPedido.ListoParaEnviar => "listo para enviar",
            EstadoPedido.Enviado => "enviado",
            EstadoPedido.Entregado => "entregado",
            EstadoPedido.Completado => "completado",
            EstadoPedido.Cancelado => "cancelado",
            _ => "pendiente de aprobación"
        };
    }
}
