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
        private readonly ILogger<PedidoComercioService>? _logger;

        public PedidoComercioService(
            IPedidoComercioRepository repository,
            JwtContext jwt,
            IAmazonS3 s3,
            IConfiguration configuration,
            INotificacionService notificaciones,
            IComisionService comisiones, ILogger<PedidoComercioService>? logger = null)
        {
            _repository = repository;
            _jwt = jwt;
            _s3 = s3;
            _bucket = configuration["R2:ComprobantesBucket"] ?? "comprobantes-pago";
            _notificaciones = notificaciones;
            _logger = logger;
        }

        public async Task<ApiResponse<List<ComercioPedidoSelectorDto>>> ObtenerComerciosAsync(CancellationToken cancellationToken = default) =>
            ApiResponse<List<ComercioPedidoSelectorDto>>.Success(
                await _repository.ObtenerComerciosAsync(_jwt.GetUserId(), _jwt.GetUserRole()));

        public async Task<ApiResponse<PedidosComercioDashboardDto>> ObtenerDashboardAsync(long comercioId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await Autorizado(comercioId))
                return ApiResponse<PedidosComercioDashboardDto>.Error("403", "No tienes acceso a este comercio.");

            return ApiResponse<PedidosComercioDashboardDto>.Success(
                await _repository.ObtenerDashboardAsync(comercioId));
        }

        public async Task<ApiResponse<PagedResponse<PedidoComercioListadoDto>>> ObtenerPedidosAsync(
            long comercioId, int page, int pageSize, EstadoPedido? estado, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await Autorizado(comercioId))
                return ApiResponse<PagedResponse<PedidoComercioListadoDto>>.Error("403", "No tienes acceso a este comercio.");

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            return ApiResponse<PagedResponse<PedidoComercioListadoDto>>.Success(
                await _repository.ObtenerPedidosAsync(comercioId, page, pageSize, estado));
        }

        public async Task<ApiResponse<PedidoComercioDetalleDto>> ObtenerDetalleAsync(
            long comercioId, Guid pedidoUuid, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await Autorizado(comercioId))
                return ApiResponse<PedidoComercioDetalleDto>.Error("403", "No tienes acceso a este comercio.");

            var pedido = await _repository.ObtenerDetalleAsync(comercioId, pedidoUuid);
            return pedido == null
                ? ApiResponse<PedidoComercioDetalleDto>.Error("404", "Pedido no encontrado.")
                : ApiResponse<PedidoComercioDetalleDto>.Success(pedido);
        }

        public Task<ApiResponse<PedidoComercioDetalleDto>> CambiarEstadoAsync(
            long comercioId, Guid pedidoUuid, CambiarEstadoPedidoDto dto, CancellationToken cancellationToken = default) =>
            OperarAsync(comercioId, pedidoUuid, () => _repository.CambiarEstadoAsync(comercioId, pedidoUuid, _jwt.GetUserId(), dto, cancellationToken),
                "Pedido actualizado", $"El pedido cambió a: {ObtenerTextoEstado(dto.Estado)}.", cancellationToken);

        public Task<ApiResponse<PedidoComercioDetalleDto>> RevisarPagoAsync(
            long comercioId, Guid pedidoUuid, RevisarPagoPedidoDto dto, CancellationToken cancellationToken = default) =>
            OperarAsync(comercioId, pedidoUuid, () => _repository.RevisarPagoAsync(comercioId, pedidoUuid, _jwt.GetUserId(), dto, cancellationToken),
                "Pago actualizado", dto.EstadoPago == EstadoPagoPedido.Pagado ? "El comercio confirmó la recepción del pago." : "El comprobante fue rechazado. Revisa el motivo en el pedido.", cancellationToken);

        public async Task<ApiResponse<PedidoComercioDetalleDto>> ReembolsarAsync(
            long comercioId, Guid pedidoUuid, RegistrarReembolsoPedidoDto dto, CancellationToken cancellationToken = default)
        {
            if (_jwt.GetUserRole() != RolesUsuario.Comercio)
                return ApiResponse<PedidoComercioDetalleDto>.Error("403", "Sólo el dueño del comercio puede registrar reembolsos.");
            return await OperarAsync(comercioId, pedidoUuid, () => _repository.ReembolsarAsync(comercioId, pedidoUuid, _jwt.GetUserId(), dto, cancellationToken),
                "Reembolso registrado", "El comercio registró la devolución del total del pedido. Revisa el método y la referencia en el detalle.", cancellationToken);
        }

        public async Task<ApiResponse<PedidoComercioDetalleDto>> ConciliarInventarioAsync(
            long comercioId, Guid pedidoUuid, ConciliarInventarioPedidoDto dto, CancellationToken cancellationToken = default)
        {
            if (_jwt.GetUserRole() != RolesUsuario.Comercio)
                return ApiResponse<PedidoComercioDetalleDto>.Error("403", "Sólo el dueño puede conciliar el inventario.");
            return await OperarAsync(comercioId, pedidoUuid, () => _repository.ConciliarInventarioAsync(comercioId, pedidoUuid, _jwt.GetUserId(), dto, cancellationToken),
                "Inventario conciliado", "El comercio revisó el inventario del pedido.", cancellationToken);
        }

        private async Task<ApiResponse<PedidoComercioDetalleDto>> OperarAsync(long comercioId, Guid uuid,
            Func<Task<Pedido>> operar, string titulo, string mensaje, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!await Autorizado(comercioId))
                return ApiResponse<PedidoComercioDetalleDto>.Error("403", "No tienes acceso a este comercio.");
            try
            {
                var pedido = await operar();
                // A notification failure must never turn a committed payment/refund into a failed operation.
                try { await _notificaciones.NotificarClienteAsync(pedido, TipoNotificacionPedido.EstadoPedidoActualizado, titulo, mensaje); }
                catch (Exception ex) { _logger?.LogWarning(ex, "No se pudo notificar la operación del pedido {PedidoUuid}", uuid); }
                return await ObtenerDetalleAsync(comercioId, uuid, ct);
            }
            catch (PedidoOperacionException ex) { return ApiResponse<PedidoComercioDetalleDto>.Error(ex.Codigo, ex.Message); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Falló la operación del pedido {PedidoUuid}", uuid);
                return ApiResponse<PedidoComercioDetalleDto>.Error("500", "No se pudo confirmar la operación. Actualiza el pedido antes de reintentar; para reembolsos conserva la misma clave y referencia.");
            }
        }

        public async Task<ApiResponse<ArchivoComprobanteDto>> ObtenerComprobanteAsync(
            long comercioId, Guid pedidoUuid, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                }, cancellationToken);
                await using var memory = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memory, cancellationToken);
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
