using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Amazon.S3;
using Amazon.S3.Model;

namespace AdLocalAPI.Services
{
    public class ComprobantePagoService : IComprobantePagoService
    {
        private const long MaximoBytes = 10 * 1024 * 1024;

        private static readonly IReadOnlyDictionary<string, string> Extensiones =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["image/jpeg"] = ".jpg",
                ["image/png"] = ".png",
                ["application/pdf"] = ".pdf"
            };

        private readonly IPedidoRepository _repository;
        private readonly JwtContext _jwtContext;
        private readonly IAmazonS3 _s3Client;
        private readonly IWebHostEnvironment _environment;
        private readonly string _bucketName;
        private readonly INotificacionService _notificaciones;

        public ComprobantePagoService(
            IPedidoRepository repository,
            JwtContext jwtContext,
            IAmazonS3 s3Client,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            INotificacionService notificaciones)
        {
            _repository = repository;
            _jwtContext = jwtContext;
            _s3Client = s3Client;
            _environment = environment;
            _bucketName = configuration["R2:ComprobantesBucket"]
                ?? "comprobantes-pago";
            _notificaciones = notificaciones;
        }

        public async Task<ApiResponse<ComprobanteTransferenciaResponseDto>>
            SubirAsync(Guid pedidoUuid, SubirComprobanteTransferenciaDto comprobanteDto)
        {
            if (comprobanteDto == null || string.IsNullOrWhiteSpace(comprobanteDto.ArchivoBase64))
            {
                return ApiResponse<ComprobanteTransferenciaResponseDto>.Error(
                    "400",
                    "Selecciona un comprobante."
                );
            }

            if (!TryDecodificar(
                    comprobanteDto.ArchivoBase64,
                    out var contenido,
                    out var contentType))
            {
                return ApiResponse<ComprobanteTransferenciaResponseDto>.Error(
                    "400",
                    "El comprobante no contiene un Base64 válido."
                );
            }

            if (contenido.LongLength > MaximoBytes)
            {
                return ApiResponse<ComprobanteTransferenciaResponseDto>.Error(
                    "400",
                    "El comprobante no puede superar 10 MB."
                );
            }

            if (!Extensiones.TryGetValue(contentType, out var extension))
            {
                return ApiResponse<ComprobanteTransferenciaResponseDto>.Error(
                    "400",
                    "El comprobante debe ser JPG, PNG o PDF."
                );
            }

            if (!FirmaValida(contenido, contentType))
            {
                return ApiResponse<ComprobanteTransferenciaResponseDto>.Error(
                    "400",
                    "El contenido del archivo no corresponde a un JPG, PNG o PDF válido."
                );
            }

            var idUsuario = _jwtContext.GetUserId();
            var pedido = await _repository.ObtenerPedidoClienteAsync(
                pedidoUuid,
                idUsuario
            );

            if (pedido == null)
            {
                return ApiResponse<ComprobanteTransferenciaResponseDto>.Error(
                    "404",
                    "No se encontró el pedido."
                );
            }

            if (pedido.MetodoPago != MetodoPagoPedido.Transferencia)
            {
                return ApiResponse<ComprobanteTransferenciaResponseDto>.Error(
                    "400",
                    "Este pedido no utiliza transferencia."
                );
            }

            if (pedido.Estado == EstadoPedido.Cancelado ||
                pedido.Estado == EstadoPedido.Rechazado)
            {
                return ApiResponse<ComprobanteTransferenciaResponseDto>.Error(
                    "400",
                    "El pedido ya no permite recibir comprobantes."
                );
            }

            if (pedido.EstadoPago == EstadoPagoPedido.PendienteVerificacion)
            {
                return ApiResponse<ComprobanteTransferenciaResponseDto>.Error(
                    "409",
                    "El comprobante ya está pendiente de verificación."
                );
            }

            if (pedido.EstadoPago != EstadoPagoPedido.PendienteComprobante &&
                pedido.EstadoPago != EstadoPagoPedido.Rechazado)
            {
                return ApiResponse<ComprobanteTransferenciaResponseDto>.Error(
                    "400",
                    "El estado del pago no permite subir un comprobante."
                );
            }

            var comprobanteUuid = Guid.NewGuid();
            var entorno = _environment.IsProduction() ? "prod" : "local";
            var key = $"comprobantes-pago/{entorno}/{pedido.Uuid}/{comprobanteUuid}{extension}";

            try
            {
                await using var stream = new MemoryStream(contenido, writable: false);

                await _s3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = contentType,
                    DisablePayloadSigning = true
                });

                var fechaCarga = DateTime.UtcNow;
                var comprobante = new ComprobantePago
                {
                    Uuid = comprobanteUuid,
                    IdPedido = pedido.Id,
                    IdUsuario = idUsuario,
                    ArchivoUrl = key,
                    Monto = pedido.Total,
                    Estatus = (int)EstadoPagoPedido.PendienteVerificacion,
                    FechaCreacion = fechaCarga,
                    Activo = true
                };

                try
                {
                    await _repository.GuardarComprobanteAsync(
                        pedido,
                        comprobante
                    );

                    await _notificaciones.NotificarComercioAsync(
                        pedido,
                        TipoNotificacionPedido.ComprobanteSubido,
                        "Comprobante recibido",
                        $"El cliente adjuntó el comprobante del pedido {pedido.NumeroPedido}."
                    );
                }
                catch
                {
                    await EliminarArchivoSilenciosamenteAsync(key);
                    throw;
                }

                return ApiResponse<ComprobanteTransferenciaResponseDto>.Success(
                    new ComprobanteTransferenciaResponseDto
                    {
                        PedidoUuid = pedido.Uuid,
                        ComprobanteUuid = comprobanteUuid,
                        EstadoPago = (int)EstadoPagoPedido.PendienteVerificacion,
                        FechaCarga = fechaCarga
                    },
                    "Comprobante enviado para verificación."
                );
            }
            catch
            {
                return ApiResponse<ComprobanteTransferenciaResponseDto>.Error(
                    "500",
                    "No fue posible guardar el comprobante. Intenta nuevamente."
                );
            }
        }

        private async Task EliminarArchivoSilenciosamenteAsync(string key)
        {
            try
            {
                await _s3Client.DeleteObjectAsync(new Amazon.S3.Model.DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                });
            }
            catch
            {
                // La limpieza del objeto huérfano no debe ocultar el error original.
            }
        }

        private static bool TryDecodificar(
            string valor,
            out byte[] contenido,
            out string contentType)
        {
            contenido = Array.Empty<byte>();
            contentType = string.Empty;

            try
            {
                var base64 = valor.Trim();
                if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    var separador = base64.IndexOf(',');
                    if (separador <= 5) return false;

                    var encabezado = base64[5..separador];
                    var partes = encabezado.Split(';');
                    if (partes.Length < 2 ||
                        !partes.Skip(1).Any(x => x.Equals("base64", StringComparison.OrdinalIgnoreCase)))
                        return false;

                    contentType = partes[0].ToLowerInvariant();
                    base64 = base64[(separador + 1)..];
                }
                else
                {
                    return false;
                }

                if (!Extensiones.ContainsKey(contentType) ||
                    base64.Length > ((MaximoBytes + 2) / 3 * 4) + 8)
                    return false;

                contenido = Convert.FromBase64String(base64);
                return contenido.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool FirmaValida(byte[] contenido, string contentType)
        {
            if (contenido.Length < 5) return false;

            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" =>
                    contenido.Length >= 3 &&
                    contenido[0] == 0xFF &&
                    contenido[1] == 0xD8 &&
                    contenido[2] == 0xFF,

                "image/png" =>
                    contenido.Length >= 8 &&
                    contenido.AsSpan(0, 8).SequenceEqual(new byte[]
                    {
                        0x89, 0x50, 0x4E, 0x47,
                        0x0D, 0x0A, 0x1A, 0x0A
                    }),

                "application/pdf" =>
                    contenido[0] == 0x25 &&
                    contenido[1] == 0x50 &&
                    contenido[2] == 0x44 &&
                    contenido[3] == 0x46 &&
                    contenido[4] == 0x2D,

                _ => false
            };
        }
    }
}
