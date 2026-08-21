using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Utils;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Controllers
{
    [ApiController, Route("api/PagosComisiones")]
    public class PagosComisionesController : ControllerBase
    {
        private readonly AppDbContext _context; private readonly JwtContext _jwt; private readonly IPedidoComercioRepository _pedidos; private readonly IAmazonS3 _s3; private readonly string _bucket;
        public PagosComisionesController(AppDbContext context, JwtContext jwt, IPedidoComercioRepository pedidos, IAmazonS3 s3, IConfiguration config)
        { _context = context; _jwt = jwt; _pedidos = pedidos; _s3 = s3; _bucket = config["R2:ComprobantesBucket"] ?? "comprobantes-pago"; }

        [Authorize(Roles = "Comercio"), HttpGet("comercio/{comercioId:long}")]
        public async Task<IActionResult> Estado(long comercioId)
        {
            if (!await _pedidos.PuedeGestionarAsync(_jwt.GetUserId(), _jwt.GetUserRole(), comercioId)) return Forbid();
            var comercio = await _context.Comercios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == comercioId); if (comercio == null) return NotFound();
            var hoy = DateTime.UtcNow.Date; var semana = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7)); var mes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var pendientes = _context.Comisiones.AsNoTracking().Where(x => x.IdComercio == comercioId && x.Activo && x.Estatus == 1 && !_context.PagosComisionesDetalle.Any(d => d.IdComision == x.Id));
            var revision = await (from p in _context.PagosComisiones.AsNoTracking() join c in _context.Comercios on p.IdComercio equals c.Id where p.IdComercio == comercioId && p.Estatus == 1 orderby p.FechaCreacion descending select Mapear(p, c.Nombre, p.Detalles.Count)).FirstOrDefaultAsync();
            return Ok(ApiResponse<EstadoComisionesComercioDto>.Success(new EstadoComisionesComercioDto { ComercioId = comercioId, Comercio = comercio.Nombre,
                PendienteSemana = await pendientes.Where(x => x.FechaCreacion >= semana).SumAsync(x => (decimal?)x.MontoComision) ?? 0,
                PendienteMes = await pendientes.Where(x => x.FechaCreacion >= mes).SumAsync(x => (decimal?)x.MontoComision) ?? 0, PagoEnRevision = revision }));
        }

        [Authorize(Roles = "Comercio"), HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearPagoComisionDto dto)
        {
            if (!await _pedidos.PuedeGestionarAsync(_jwt.GetUserId(), _jwt.GetUserRole(), dto.ComercioId)) return Forbid();
            if (dto.MetodoPago != "transferencia" && dto.MetodoPago != "deposito") return BadRequest(ApiResponse<object>.Error("400", "Método de pago inválido."));
            if (await _context.PagosComisiones.AnyAsync(x => x.IdComercio == dto.ComercioId && x.Estatus == 1)) return Conflict(ApiResponse<object>.Error("409", "Ya existe un pago pendiente de verificación."));
            var cuenta = await _context.CuentasBancariasAdLocal.FirstOrDefaultAsync(x => x.Uuid == dto.CuentaBancariaUuid && x.Activo); if (cuenta == null) return BadRequest(ApiResponse<object>.Error("400", "La cuenta bancaria ya no está disponible."));
            if (!Decodificar(dto.ComprobanteBase64, out var bytes, out var contentType, out var extension)) return BadRequest(ApiResponse<object>.Error("400", "El comprobante debe ser JPG, PNG o PDF y no superar 10 MB."));
            var desde = Desde(dto.Periodo); var comisiones = await _context.Comisiones.Where(x => x.IdComercio == dto.ComercioId && x.Activo && x.Estatus == 1 && x.FechaCreacion >= desde && !_context.PagosComisionesDetalle.Any(d => d.IdComision == x.Id)).ToListAsync();
            if (comisiones.Count == 0) return BadRequest(ApiResponse<object>.Error("400", "No hay comisiones pendientes para este periodo."));
            var uuid = Guid.NewGuid(); var key = $"pagos-comisiones/{dto.ComercioId}/{uuid}{extension}";
            await using var stream = new MemoryStream(bytes, false); await _s3.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest { BucketName = _bucket, Key = key, InputStream = stream, ContentType = contentType, DisablePayloadSigning = true });
            var pago = new PagoComision { Uuid = uuid, IdComercio = dto.ComercioId, IdCuentaBancariaAdLocal = cuenta.Id, Periodo = dto.Periodo == "mes" ? "mes" : "semana", MetodoPago = dto.MetodoPago, Monto = comisiones.Sum(x => x.MontoComision), ComprobanteUrl = key, Estatus = 1, IdUsuarioCreacion = _jwt.GetUserId(), Detalles = comisiones.Select(x => new PagoComisionDetalle { IdComision = x.Id, Monto = x.MontoComision }).ToList() };
            _context.PagosComisiones.Add(pago); await _context.SaveChangesAsync(); return Ok(ApiResponse<PagoComisionListadoDto>.Success(Mapear(pago, "", pago.Detalles.Count), "Pago enviado para verificación."));
        }

        [Authorize(Roles = "Admin"), HttpGet("admin")]
        public async Task<IActionResult> ListarAdmin([FromQuery] int? estatus = null) => Ok(ApiResponse<List<PagoComisionListadoDto>>.Success(await (from p in _context.PagosComisiones.AsNoTracking() join c in _context.Comercios on p.IdComercio equals c.Id where !estatus.HasValue || p.Estatus == estatus orderby p.FechaCreacion descending select Mapear(p, c.Nombre, p.Detalles.Count)).ToListAsync()));

        [Authorize(Roles = "Admin"), HttpPut("{uuid:guid}/revisar")]
        public async Task<IActionResult> Revisar(Guid uuid, [FromBody] RevisarPagoComisionDto dto)
        {
            var pago = await _context.PagosComisiones.Include(x => x.Detalles).FirstOrDefaultAsync(x => x.Uuid == uuid); if (pago == null) return NotFound(); if (pago.Estatus != 1) return Conflict(ApiResponse<object>.Error("409", "El pago ya fue revisado."));
            pago.Estatus = dto.Aprobar ? 2 : 3; pago.Comentario = dto.Comentario?.Trim(); pago.IdUsuarioRevision = _jwt.GetUserId(); pago.FechaRevision = DateTime.UtcNow;
            if (dto.Aprobar) { var ids = pago.Detalles.Select(x => x.IdComision).ToList(); var comisiones = await _context.Comisiones.Where(x => ids.Contains(x.Id)).ToListAsync(); foreach (var c in comisiones) { c.Estatus = (int)EstatusComision.Pagada; c.FechaPago = pago.FechaRevision; } }
            else { _context.PagosComisionesDetalle.RemoveRange(pago.Detalles); }
            await _context.SaveChangesAsync(); return Ok(ApiResponse<object>.Success(null, dto.Aprobar ? "Pago aprobado." : "Pago rechazado."));
        }

        [Authorize, HttpGet("{uuid:guid}/comprobante")]
        public async Task<IActionResult> Comprobante(Guid uuid)
        {
            var pago = await _context.PagosComisiones.AsNoTracking().FirstOrDefaultAsync(x => x.Uuid == uuid); if (pago == null) return NotFound();
            if (!_jwt.GetUserRole().Equals("Admin", StringComparison.OrdinalIgnoreCase) && !await _pedidos.PuedeGestionarAsync(_jwt.GetUserId(), _jwt.GetUserRole(), pago.IdComercio)) return Forbid();
            using var objeto = await _s3.GetObjectAsync(new Amazon.S3.Model.GetObjectRequest { BucketName = _bucket, Key = pago.ComprobanteUrl }); await using var memory = new MemoryStream(); await objeto.ResponseStream.CopyToAsync(memory); return File(memory.ToArray(), objeto.Headers.ContentType ?? "application/octet-stream");
        }

        private static DateTime Desde(string periodo) { var h = DateTime.UtcNow.Date; return periodo == "mes" ? new DateTime(h.Year, h.Month, 1, 0, 0, 0, DateTimeKind.Utc) : h.AddDays(-(((int)h.DayOfWeek + 6) % 7)); }
        private static PagoComisionListadoDto Mapear(PagoComision p, string comercio, int detalles) => new() { Uuid = p.Uuid, ComercioId = p.IdComercio, Comercio = comercio, Periodo = p.Periodo, MetodoPago = p.MetodoPago, Monto = p.Monto, Estatus = p.Estatus, Comentario = p.Comentario, FechaCreacion = p.FechaCreacion, ComisionesIncluidas = detalles };
        private static bool Decodificar(string valor, out byte[] bytes, out string tipo, out string ext) { bytes = Array.Empty<byte>(); tipo = ext = ""; try { var coma = valor.IndexOf(','); if (!valor.StartsWith("data:") || coma < 0) return false; tipo = valor[5..valor.IndexOf(';')].ToLowerInvariant(); ext = tipo switch { "image/jpeg" => ".jpg", "image/png" => ".png", "application/pdf" => ".pdf", _ => "" }; if (ext == "") return false; var b64 = valor[(coma + 1)..]; if (b64.Length > 14_000_000) return false; bytes = Convert.FromBase64String(b64); return bytes.Length > 0 && bytes.Length <= 10 * 1024 * 1024; } catch { return false; } }
    }
}
