using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories;

public partial class PedidoComercioRepository
{
    private async Task<Pedido> OperarAsync(long comercio, Guid uuid, CancellationToken ct, Func<Pedido, Task> operar)
    {
        // All order writers (including proof upload and commission reconciliation) lock this same row.
        _context.ChangeTracker.Clear();
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var pedido = await _context.Pedidos.FromSqlInterpolated(
                $"SELECT * FROM pedidos WHERE \"IdComercio\" = {comercio} AND \"Uuid\" = {uuid} FOR UPDATE")
                .AsTracking().SingleOrDefaultAsync(ct);
            if (pedido == null) throw new PedidoOperacionException("404", "Pedido no encontrado.");
            await _context.Entry(pedido).Collection(x => x.Detalles).Query().AsTracking().LoadAsync(ct);
            await operar(pedido);
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return pedido;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            _context.ChangeTracker.Clear();
            throw;
        }
    }

    public Task<Pedido> CambiarEstadoAsync(long idComercio, Guid uuid, long actor, CambiarEstadoPedidoDto dto, CancellationToken ct) =>
        OperarAsync(idComercio, uuid, ct, async p =>
        {
            PedidoOperacion.ValidarEstado(p, dto);
            var anterior = p.Estado;
            p.Estado = dto.Estado;
            if (dto.Estado == EstadoPedido.Aprobado) p.FechaAprobacion = DateTime.UtcNow;
            if (dto.Estado == EstadoPedido.Entregado) p.FechaEntrega = DateTime.UtcNow;
            if (dto.Estado == EstadoPedido.Completado) p.FechaFinalizacion = DateTime.UtcNow;
            if (PedidoOperacion.Cerrado(dto.Estado))
            {
                p.FechaFinalizacion = DateTime.UtcNow;
                p.RestituirReserva = !PedidoOperacion.RequiereDevolucionFisica(new Pedido { Estado = anterior, FechaEntrega = p.FechaEntrega });
                if (p.RestituirReserva == true)
                {
                    PedidoOperacion.Exigir(dto.ProductosRestituibles == null || dto.ProductosRestituibles.Count == 0,
                        "El stock reservado se libera automáticamente antes de la preparación.", "400");
                    foreach (var d in p.Detalles) d.StockPorRestituir = d.StockDescontado ?? 0;
                }
                else
                {
                    PedidoOperacion.Exigir(dto.ProductosRestituibles != null,
                        "Revisa e indica las unidades recuperadas y aptas para venta; utiliza una lista vacía si no hay ninguna.", "400");
                    PedidoOperacion.DeclararDevolucion(p, dto.ProductosRestituibles);
                }
                await RestituirAsync(p, actor, ct);
            }
            Historial(p, actor, "Estado", dto.Comentario, anterior);
        });

    public Task<Pedido> RevisarPagoAsync(long idComercio, Guid uuid, long actor, RevisarPagoPedidoDto dto, CancellationToken ct) =>
        OperarAsync(idComercio, uuid, ct, async p =>
        {
            PedidoOperacion.Version(p, dto.VersionPedido);
            PedidoOperacion.Exigir(dto.EstadoPago is EstadoPagoPedido.Pagado or EstadoPagoPedido.Rechazado, "Estado de pago no válido.", "400");
            PedidoOperacion.Exigir(PedidoOperacion.PuedeRegistrarPago(p), "Este pedido ya no permite registrar o revisar pagos.");
            PedidoOperacion.Exigir(dto.Comentario?.Length is not > 500, "El comentario excede 500 caracteres.", "400");
            if (p.MetodoPago == MetodoPagoPedido.Transferencia)
            {
                PedidoOperacion.Exigir(p.EstadoPago == EstadoPagoPedido.PendienteVerificacion, "El pago no está pendiente de verificación.");
                var proof = await _context.ComprobantesPago.AsTracking().Where(x => x.IdPedido == p.Id && x.Activo)
                    .OrderByDescending(x => x.FechaCreacion).FirstOrDefaultAsync(ct);
                if (proof == null) throw new PedidoOperacionException("404", "No se encontró el comprobante.");
                if (dto.EstadoPago == EstadoPagoPedido.Rechazado)
                    PedidoOperacion.Exigir(!string.IsNullOrWhiteSpace(dto.Comentario), "Escribe el motivo del rechazo.", "400");
                proof.Estatus = (int)dto.EstadoPago;
                proof.IdUsuarioValidacion = actor; proof.FechaValidacion = DateTime.UtcNow; proof.Comentario = dto.Comentario?.Trim();
            }
            else PedidoOperacion.Exigir(dto.EstadoPago == EstadoPagoPedido.Pagado, "Un pago en efectivo no utiliza comprobante.", "400");
            var anterior = p.EstadoPago;
            p.EstadoPago = dto.EstadoPago;
            if (dto.EstadoPago == EstadoPagoPedido.Pagado && p.MontoComision > 0 &&
                !await _context.Comisiones.AnyAsync(x => x.TipoOperacion == (int)TipoOperacionComision.Venta && x.IdReferencia == p.Id, ct))
                _context.Comisiones.Add(new Comision { IdComercio = p.IdComercio, IdReferencia = p.Id,
                    TipoOperacion = (int)TipoOperacionComision.Venta, MontoOperacion = p.Total,
                    PorcentajeComision = p.PorcentajeComision, MontoComision = p.MontoComision,
                    Observaciones = $"Venta {p.NumeroPedido}." });
            Historial(p, actor, "Pago", dto.Comentario, pagoAnterior: anterior);
        });

    public Task<Pedido> ReembolsarAsync(long idComercio, Guid uuid, long actor, RegistrarReembolsoPedidoDto dto, CancellationToken ct) =>
        OperarAsync(idComercio, uuid, ct, async p =>
        {
            PedidoOperacion.Exigir(dto.IdempotencyKey != Guid.Empty && dto.ProductosDevueltos != null && dto.ProductosDevueltos.All(x => x != null) &&
                !string.IsNullOrWhiteSpace(dto.Referencia) && !string.IsNullOrWhiteSpace(dto.Motivo) &&
                dto.Referencia.Length <= 150 && dto.Motivo.Length <= 500 && Enum.IsDefined(dto.Metodo),
                "Indica clave, método, referencia y motivo válidos del reembolso realizado.", "400");
            var hash = PedidoOperacion.HashReembolso(dto);
            var existente = await _context.PedidoReembolsos.SingleOrDefaultAsync(x => x.IdPedido == p.Id, ct);
            if (existente != null)
            {
                PedidoOperacion.Exigir(existente.IdempotencyKey == dto.IdempotencyKey && existente.RequestHash == hash,
                    "Ya existe un reembolso para este pedido; revisa su referencia.");
                return; // Retry after a lost HTTP response, including after a later inventory reconciliation.
            }
            PedidoOperacion.Version(p, dto.VersionPedido);
            PedidoOperacion.Exigir(p.EstadoPago == EstadoPagoPedido.Pagado, "Sólo puede reembolsarse un pedido pagado.");
            PedidoOperacion.Exigir(dto.Monto == p.Total && dto.Monto > 0, "El reembolso debe corresponder al total exacto del pedido.", "400");
            PedidoOperacion.Exigir(dto.ProductosDevueltos.Select(x => x.DetalleUuid).Distinct().Count() == dto.ProductosDevueltos.Count,
                "No repitas productos en la devolución.", "400");
            var entregado = PedidoOperacion.Entregado(p.Estado) || p.FechaEntrega.HasValue;
            var fisica = PedidoOperacion.RequiereDevolucionFisica(p) || p.RestituirReserva == false;
            PedidoOperacion.Exigir(fisica || dto.ProductosDevueltos.Count == 0,
                "Antes de preparar, el stock reservado se libera automáticamente; no declares devoluciones.", "400");
            PedidoOperacion.DeclararDevolucion(p, dto.ProductosDevueltos);
            var anterior = p.Estado;
            if (!entregado)
            {
                p.Estado = EstadoPedido.Cancelado; p.FechaFinalizacion = DateTime.UtcNow;
                p.RestituirReserva = !fisica;
                if (!fisica) foreach (var d in p.Detalles) d.StockPorRestituir = d.StockDescontado ?? 0;
            }
            await RestituirAsync(p, actor, ct);
            var comision = await _context.Comisiones.FromSqlInterpolated(
                $"SELECT * FROM comisiones WHERE \"TipoOperacion\" = {1} AND \"IdReferencia\" = {p.Id} FOR UPDATE")
                .AsTracking().SingleOrDefaultAsync(ct);
            if (comision != null && comision.Estatus != (int)EstatusComision.Cancelada)
            {
                // Never rewrite a settlement or a commission already submitted in a payment batch.
                var incluida = await _context.PagosComisionesDetalle.AnyAsync(x => x.IdComision == comision.Id, ct);
                if (comision.Estatus == (int)EstatusComision.Pendiente && !incluida)
                {
                    comision.Estatus = (int)EstatusComision.Cancelada; comision.FechaCancelacion = DateTime.UtcNow;
                }
                else _context.Comisiones.Add(new Comision { IdComercio = p.IdComercio, IdReferencia = p.Id,
                    TipoOperacion = (int)TipoOperacionComision.Reembolso, MontoOperacion = -p.Total,
                    PorcentajeComision = p.PorcentajeComision, MontoComision = -comision.MontoComision,
                    Estatus = comision.Estatus == (int)EstatusComision.Pagada ? (int)EstatusComision.Pendiente : (int)EstatusComision.Retenida,
                    Observaciones = $"Crédito por reembolso {p.NumeroPedido}. Referencia: {dto.Referencia.Trim()}" });
            }
            _context.PedidoReembolsos.Add(new PedidoReembolso { IdPedido = p.Id, IdempotencyKey = dto.IdempotencyKey,
                RequestHash = hash, Monto = p.Total, Metodo = dto.Metodo, Referencia = dto.Referencia.Trim(),
                Motivo = dto.Motivo.Trim(), IdUsuarioRegistro = actor });
            var pagoAnterior = p.EstadoPago; p.EstadoPago = EstadoPagoPedido.Reembolsado;
            Historial(p, actor, "Reembolso", dto.Motivo.Trim(), anterior, pagoAnterior);
        });

    public Task<Pedido> ConciliarInventarioAsync(long idComercio, Guid uuid, long actor, ConciliarInventarioPedidoDto dto, CancellationToken ct) =>
        OperarAsync(idComercio, uuid, ct, async p =>
        {
            PedidoOperacion.Version(p, dto.VersionPedido);
            PedidoOperacion.Exigir(!string.IsNullOrWhiteSpace(dto.Referencia) && dto.Referencia.Length <= 500 && dto.StockDescontado != null && dto.StockDescontado.All(x => x != null),
                "Indica la evidencia o motivo de la conciliación.", "400");
            if (PedidoOperacion.Cerrado(p.Estado))
            {
                if (p.RestituirReserva == null)
                {
                    PedidoOperacion.Exigir(dto.RestituirReserva.HasValue, "Indica si el pedido antiguo se canceló antes de preparar y corresponde liberar su reserva.", "400");
                    p.RestituirReserva = dto.RestituirReserva;
                    if (p.RestituirReserva == true)
                        foreach (var d in p.Detalles) d.StockPorRestituir = d.StockDescontado ?? 0;
                }
                else PedidoOperacion.Exigir(dto.RestituirReserva == null || dto.RestituirReserva == p.RestituirReserva,
                    "La política de reposición ya fue registrada y no puede modificarse.");
            }
            PedidoOperacion.Exigir(dto.StockDescontado!.Select(x => x.DetalleUuid).Distinct().Count() == dto.StockDescontado.Count,
                "No repitas detalles en la conciliación.", "400");
            foreach (var dato in dto.StockDescontado)
            {
                var d = p.Detalles.SingleOrDefault(x => x.Uuid == dato.DetalleUuid);
                PedidoOperacion.Exigir(d != null && d.StockDescontado == null && (dato.Cantidad == 0 || dato.Cantidad == d.Cantidad),
                    "Sólo puede conciliarse un detalle antiguo indicando cero o la cantidad originalmente descontada.", "400");
                d!.StockDescontado = dato.Cantidad;
                if (PedidoOperacion.Cerrado(p.Estado) && p.RestituirReserva == true) d.StockPorRestituir = dato.Cantidad;
            }
            await RestituirAsync(p, actor, ct);
            Historial(p, actor, "Inventario", dto.Referencia.Trim());
        });

    private async Task RestituirAsync(Pedido p, long actor, CancellationToken ct)
    {
        // Lock products in a stable order, matching checkout's order. Arithmetic updates cannot lose units.
        foreach (var d in p.Detalles.OrderBy(x => x.IdProductoServicio).ThenBy(x => x.Id))
        {
            var unidades = d.StockPorRestituir - d.StockRestituido;
            if (unidades <= 0 || !d.IdProductoServicio.HasValue) continue;
            var afectadas = await _context.ProductosServicios.Where(x => x.Id == d.IdProductoServicio &&
                x.Uuid == d.ProductoUuid && x.IdComercio == p.IdComercio && x.Stock != null && x.Stock <= int.MaxValue - unidades)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, x => x.Stock + unidades)
                    .SetProperty(x => x.FechaActualizacion, DateTime.UtcNow), ct);
            if (afectadas == 0) continue; // Deleted/disabled inventory: visible pending reconciliation, never fabricated stock.
            d.StockRestituido += unidades;
            _context.PedidoMovimientosInventario.Add(new PedidoMovimientoInventario { IdPedidoDetalle = d.Id, Cantidad = unidades, IdUsuario = actor });
        }
    }

    private void Historial(Pedido p, long actor, string evento, string? comentario, EstadoPedido? anterior = null, EstadoPagoPedido? pagoAnterior = null)
    {
        p.Version++; p.FechaActualizacion = DateTime.UtcNow;
        _context.Add(new PedidoHistorialEstado { IdPedido = p.Id, IdUsuarioCambio = actor, Evento = evento,
            EstadoAnterior = anterior ?? p.Estado, EstadoNuevo = p.Estado,
            EstadoPagoAnterior = pagoAnterior, EstadoPagoNuevo = pagoAnterior.HasValue ? p.EstadoPago : null,
            Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim() });
    }
}
