using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace AdLocalAPI.Services
{
    public class PagoComisionService : IPagoComisionService
    {
        private readonly IPagoComisionRepository _repository;
        private readonly IPedidoComercioRepository _pedidos;
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;

        public PagoComisionService(
            IPagoComisionRepository repository,
            IPedidoComercioRepository pedidos,
            IAmazonS3 s3,
            IConfiguration config)
        {
            _repository = repository;
            _pedidos = pedidos;
            _s3 = s3;
            _bucket = config["R2:ComprobantesBucket"] ?? "comprobantes-pago";
        }

        public async Task<ApiResponse<EstadoComisionesComercioDto>> ObtenerEstadoAsync(long userId, string userRole, long comercioId)
        {
            if (!await _pedidos.PuedeGestionarAsync(userId, userRole, comercioId))
            {
                return ApiResponse<EstadoComisionesComercioDto>.Error("403", "No tienes permisos para gestionar este comercio.");
            }

            var comercio = await _repository.ObtenerComercioPorIdAsync(comercioId);
            if (comercio == null)
            {
                return ApiResponse<EstadoComisionesComercioDto>.Error("404", "Comercio no encontrado.");
            }

            var hoy = DateTime.UtcNow.Date;
            var semana = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
            var mes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var pendienteSemana = await _repository.ObtenerSumaComisionesPendientesAsync(comercioId, semana);
            var pendienteMes = await _repository.ObtenerSumaComisionesPendientesAsync(comercioId, mes);
            var revision = await _repository.ObtenerPagoEnRevisionPorComercioAsync(comercioId);

            return ApiResponse<EstadoComisionesComercioDto>.Success(new EstadoComisionesComercioDto
            {
                ComercioId = comercioId,
                Comercio = comercio.Nombre,
                PendienteSemana = pendienteSemana,
                PendienteMes = pendienteMes,
                PagoEnRevision = revision
            });
        }

        public async Task<ApiResponse<PagoComisionListadoDto>> CrearPagoAsync(long userId, string userRole, CrearPagoComisionDto dto)
        {
            if (!await _pedidos.PuedeGestionarAsync(userId, userRole, dto.ComercioId))
            {
                return ApiResponse<PagoComisionListadoDto>.Error("403", "No tienes permisos para gestionar este comercio.");
            }

            if (dto.MetodoPago != "transferencia" && dto.MetodoPago != "deposito")
            {
                return ApiResponse<PagoComisionListadoDto>.Error("400", "Método de pago inválido.");
            }

            if (await _repository.TienePagoEnRevisionAsync(dto.ComercioId))
            {
                return ApiResponse<PagoComisionListadoDto>.Error("409", "Ya existe un pago pendiente de verificación.");
            }

            var cuenta = await _repository.ObtenerCuentaBancariaActivaPorUuidAsync(dto.CuentaBancariaUuid);
            if (cuenta == null)
            {
                return ApiResponse<PagoComisionListadoDto>.Error("400", "La cuenta bancaria ya no está disponible.");
            }

            if (!Decodificar(dto.ComprobanteBase64, out var bytes, out var contentType, out var extension))
            {
                return ApiResponse<PagoComisionListadoDto>.Error("400", "El comprobante debe ser JPG, PNG o PDF y no superar 10 MB.");
            }

            var desde = Desde(dto.Periodo);
            var comisiones = await _repository.ObtenerComisionesPendientesAsync(dto.ComercioId, desde);
            if (comisiones.Count == 0)
            {
                return ApiResponse<PagoComisionListadoDto>.Error("400", "No hay comisiones pendientes para este periodo.");
            }

            var uuid = Guid.NewGuid();
            var key = $"pagos-comisiones/{dto.ComercioId}/{uuid}{extension}";

            await using var stream = new MemoryStream(bytes, false);
            await _s3.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                DisablePayloadSigning = true
            });

            var pago = new PagoComision
            {
                Uuid = uuid,
                IdComercio = dto.ComercioId,
                IdCuentaBancariaAdLocal = cuenta.Id,
                Periodo = dto.Periodo == "mes" ? "mes" : "semana",
                MetodoPago = dto.MetodoPago,
                Monto = comisiones.Sum(x => x.MontoComision),
                ComprobanteUrl = key,
                Estatus = 1,
                IdUsuarioCreacion = userId,
                Detalles = comisiones.Select(x => new PagoComisionDetalle
                {
                    IdComision = x.Id,
                    Monto = x.MontoComision
                }).ToList()
            };

            await _repository.CrearPagoAsync(pago);

            var listadoDto = new PagoComisionListadoDto
            {
                Uuid = pago.Uuid,
                ComercioId = pago.IdComercio,
                Comercio = string.Empty,
                Periodo = pago.Periodo,
                MetodoPago = pago.MetodoPago,
                Monto = pago.Monto,
                Estatus = pago.Estatus,
                Comentario = pago.Comentario,
                FechaCreacion = pago.FechaCreacion,
                ComisionesIncluidas = pago.Detalles.Count
            };

            return ApiResponse<PagoComisionListadoDto>.Success(listadoDto, "Pago enviado para verificación.");
        }

        public async Task<ApiResponse<List<PagoComisionListadoDto>>> ListarAdminAsync(int? estatus = null)
        {
            var listado = await _repository.ListarAdminAsync(estatus);
            return ApiResponse<List<PagoComisionListadoDto>>.Success(listado);
        }

        public async Task<ApiResponse<object>> RevisarPagoAsync(long userIdRevision, Guid uuid, RevisarPagoComisionDto dto)
        {
            var pago = await _repository.ObtenerPorUuidAsync(uuid, incluirDetalles: true);
            if (pago == null)
            {
                return ApiResponse<object>.Error("404", "Pago no encontrado.");
            }

            if (pago.Estatus != 1)
            {
                return ApiResponse<object>.Error("409", "El pago ya fue revisado.");
            }

            pago.Estatus = dto.Aprobar ? 2 : 3;
            pago.Comentario = dto.Comentario?.Trim();
            pago.IdUsuarioRevision = userIdRevision;
            pago.FechaRevision = DateTime.UtcNow;

            if (dto.Aprobar)
            {
                var ids = pago.Detalles.Select(x => x.IdComision).ToList();
                await _repository.MarcarComisionesComoPagadasAsync(ids, pago.FechaRevision.Value);
            }
            else
            {
                await _repository.EliminarDetallesAsync(pago.Detalles);
            }

            await _repository.ActualizarPagoAsync(pago);

            return ApiResponse<object>.Success(null, dto.Aprobar ? "Pago aprobado." : "Pago rechazado.");
        }

        public async Task<ComprobanteArchivoDto> ObtenerComprobanteAsync(long userId, string userRole, Guid uuid)
        {
            var pago = await _repository.ObtenerPorUuidAsync(uuid);
            if (pago == null)
            {
                return new ComprobanteArchivoDto
                {
                    CodigoError = "404",
                    MensajeError = "Pago no encontrado."
                };
            }

            if (!userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                !await _pedidos.PuedeGestionarAsync(userId, userRole, pago.IdComercio))
            {
                return new ComprobanteArchivoDto
                {
                    CodigoError = "403",
                    MensajeError = "No tienes permisos para ver este comprobante."
                };
            }

            using var objeto = await _s3.GetObjectAsync(new Amazon.S3.Model.GetObjectRequest
            {
                BucketName = _bucket,
                Key = pago.ComprobanteUrl
            });

            await using var memory = new MemoryStream();
            await objeto.ResponseStream.CopyToAsync(memory);

            return new ComprobanteArchivoDto
            {
                Contenido = memory.ToArray(),
                ContentType = objeto.Headers.ContentType ?? "application/octet-stream"
            };
        }

        private static DateTime Desde(string periodo)
        {
            var h = DateTime.UtcNow.Date;
            return periodo == "mes"
                ? new DateTime(h.Year, h.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                : h.AddDays(-(((int)h.DayOfWeek + 6) % 7));
        }

        private static bool Decodificar(string valor, out byte[] bytes, out string tipo, out string ext)
        {
            bytes = Array.Empty<byte>();
            tipo = ext = "";
            try
            {
                if (string.IsNullOrWhiteSpace(valor)) return false;
                var base64 = valor.Trim();
                var coma = base64.IndexOf(',');
                if (!base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || coma < 0) return false;

                var encabezado = base64[5..coma];
                var partes = encabezado.Split(';');
                if (partes.Length < 2 || !partes.Skip(1).Any(x => x.Equals("base64", StringComparison.OrdinalIgnoreCase)))
                    return false;

                tipo = partes[0].ToLowerInvariant();
                ext = tipo switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "application/pdf" => ".pdf",
                    _ => ""
                };
                if (string.IsNullOrEmpty(ext)) return false;

                var b64Data = base64[(coma + 1)..];
                if (b64Data.Length > 14_000_000) return false;

                bytes = Convert.FromBase64String(b64Data);
                if (bytes.Length == 0 || bytes.Length > 10 * 1024 * 1024) return false;

                return FirmaValida(bytes, tipo);
            }
            catch
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
