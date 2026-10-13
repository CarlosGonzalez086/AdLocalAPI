using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class PagoComisionRepository : IPagoComisionRepository
    {
        private readonly AppDbContext _context;

        public PagoComisionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Comercio?> ObtenerComercioPorIdAsync(long comercioId)
        {
            return await _context.Comercios
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == comercioId);
        }

        public async Task<decimal> ObtenerSumaComisionesPendientesAsync(long comercioId, DateTime desde)
        {
            return await _context.Comisiones
                .AsNoTracking()
                .Where(x => x.IdComercio == comercioId && x.Activo && x.Estatus == 1 && (x.FechaCreacion >= desde || x.TipoOperacion == (int)TipoOperacionComision.Reembolso) && !_context.PagosComisionesDetalle.Any(d => d.IdComision == x.Id))
                .SumAsync(x => (decimal?)x.MontoComision) ?? 0;
        }

        public async Task<PagoComisionListadoDto?> ObtenerPagoEnRevisionPorComercioAsync(long comercioId)
        {
            return await (from p in _context.PagosComisiones.AsNoTracking()
                          join c in _context.Comercios on p.IdComercio equals c.Id
                          where p.IdComercio == comercioId && p.Estatus == 1
                          orderby p.FechaCreacion descending
                          select new PagoComisionListadoDto
                          {
                              Uuid = p.Uuid,
                              ComercioId = p.IdComercio,
                              Comercio = c.Nombre,
                              Periodo = p.Periodo,
                              MetodoPago = p.MetodoPago,
                              Monto = p.Monto,
                              Estatus = p.Estatus,
                              Comentario = p.Comentario,
                              FechaCreacion = p.FechaCreacion,
                              ComisionesIncluidas = p.Detalles.Count
                          }).FirstOrDefaultAsync();
        }

        public async Task<bool> TienePagoEnRevisionAsync(long comercioId)
        {
            return await _context.PagosComisiones
                .AnyAsync(x => x.IdComercio == comercioId && x.Estatus == 1);
        }

        public async Task<CuentaBancariaAdLocal?> ObtenerCuentaBancariaActivaPorUuidAsync(Guid uuid)
        {
            return await _context.CuentasBancariasAdLocal
                .FirstOrDefaultAsync(x => x.Uuid == uuid && x.Activo);
        }

        public async Task<List<Comision>> ObtenerComisionesPendientesAsync(long comercioId, DateTime desde)
        {
            return await _context.Comisiones
                .Where(x => x.IdComercio == comercioId && x.Activo && x.Estatus == 1 && (x.FechaCreacion >= desde || x.TipoOperacion == (int)TipoOperacionComision.Reembolso) && !_context.PagosComisionesDetalle.Any(d => d.IdComision == x.Id))
                .ToListAsync();
        }

        public async Task<PagoComision> CrearPagoAsync(PagoComision pago)
        {
            await using var tx = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync() : null;
            if (_context.Database.IsRelational())
                await _context.Comercios.FromSqlInterpolated($"SELECT * FROM \"Comercios\" WHERE \"Id\" = {pago.IdComercio} FOR UPDATE").AsNoTracking().SingleAsync();
            PedidoOperacion.Exigir(!await TienePagoEnRevisionAsync(pago.IdComercio), "Ya existe un pago de comisiones en revisión.");
            var comisiones = await BloquearAsync(pago.Detalles.Select(x => x.IdComision));
            PedidoOperacion.Exigir(comisiones.All(x => x.IdComercio == pago.IdComercio && x.Activo && x.Estatus == 1) &&
                !await _context.PagosComisionesDetalle.AnyAsync(x => pago.Detalles.Select(d => d.IdComision).Contains(x.IdComision)),
                "Las comisiones cambiaron; actualiza el saldo antes de enviar el pago.");
            PedidoOperacion.Exigir(comisiones.Sum(x => x.MontoComision) == pago.Monto && pago.Monto > 0,
                "El monto de las comisiones cambió o está cubierto por créditos.");
            _context.PagosComisiones.Add(pago); await _context.SaveChangesAsync();
            if (tx != null) await tx.CommitAsync();
            return pago;
        }

        public async Task<List<PagoComisionListadoDto>> ListarAdminAsync(int? estatus = null)
        {
            return await (from p in _context.PagosComisiones.AsNoTracking()
                          join c in _context.Comercios on p.IdComercio equals c.Id
                          where !estatus.HasValue || p.Estatus == estatus
                          orderby p.FechaCreacion descending
                          select new PagoComisionListadoDto
                          {
                              Uuid = p.Uuid,
                              ComercioId = p.IdComercio,
                              Comercio = c.Nombre,
                              Periodo = p.Periodo,
                              MetodoPago = p.MetodoPago,
                              Monto = p.Monto,
                              Estatus = p.Estatus,
                              Comentario = p.Comentario,
                              FechaCreacion = p.FechaCreacion,
                              ComisionesIncluidas = p.Detalles.Count
                          }).ToListAsync();
        }

        public async Task<PagoComision?> ObtenerPorUuidAsync(Guid uuid, bool incluirDetalles = false)
        {
            IQueryable<PagoComision> query = _context.PagosComisiones;
            if (incluirDetalles)
            {
                query = query.Include(x => x.Detalles);
            }
            else
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(x => x.Uuid == uuid);
        }

        public async Task ActualizarPagoAsync(PagoComision pago)
        {
            _context.ChangeTracker.Clear();
            await using var tx = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync() : null;
            var actual = _context.Database.IsRelational()
                ? await _context.PagosComisiones.FromSqlInterpolated($"SELECT * FROM pagos_comisiones WHERE \"Id\" = {pago.Id} FOR UPDATE").AsTracking().SingleAsync()
                : await _context.PagosComisiones.AsTracking().SingleAsync(x => x.Id == pago.Id);
            PedidoOperacion.Exigir(actual.Estatus == 1, "El pago ya fue revisado.");
            await _context.Entry(actual).Collection(x => x.Detalles).Query().AsTracking().LoadAsync();
            var comisiones = await BloquearAsync(actual.Detalles.Select(x => x.IdComision));
            PedidoOperacion.Exigir(comisiones.All(x => x.Activo && x.Estatus == 1 && x.IdComercio == actual.IdComercio),
                "Las comisiones del pago cambiaron y requieren revisión.");
            if (pago.Estatus == 2)
            {
                foreach (var c in comisiones)
                {
                    c.Estatus = 2; c.FechaPago = pago.FechaRevision;
                    var credito = _context.Database.IsRelational()
                        ? await _context.Comisiones.FromSqlInterpolated($"SELECT * FROM comisiones WHERE \"TipoOperacion\" = {2} AND \"IdReferencia\" = {c.IdReferencia} FOR UPDATE").AsTracking().SingleOrDefaultAsync()
                        : await _context.Comisiones.AsTracking().SingleOrDefaultAsync(x => x.TipoOperacion == 2 && x.IdReferencia == c.IdReferencia);
                    if (credito?.Estatus == (int)EstatusComision.Retenida) credito.Estatus = (int)EstatusComision.Pendiente;
                }
            }
            else
            {
                foreach (var c in comisiones.Where(x => x.TipoOperacion == (int)TipoOperacionComision.Venta))
                {
                    var credito = _context.Database.IsRelational()
                        ? await _context.Comisiones.FromSqlInterpolated($"SELECT * FROM comisiones WHERE \"TipoOperacion\" = {2} AND \"IdReferencia\" = {c.IdReferencia} FOR UPDATE").AsTracking().SingleOrDefaultAsync()
                        : await _context.Comisiones.AsTracking().SingleOrDefaultAsync(x => x.TipoOperacion == 2 && x.IdReferencia == c.IdReferencia);
                    if (credito?.Estatus == (int)EstatusComision.Retenida)
                    {
                        // No commission was received: void both sides instead of granting unearned credit.
                        c.Estatus = (int)EstatusComision.Cancelada; c.FechaCancelacion = DateTime.UtcNow;
                        credito.Estatus = (int)EstatusComision.Cancelada; credito.FechaCancelacion = DateTime.UtcNow;
                    }
                }
                _context.PagosComisionesDetalle.RemoveRange(actual.Detalles);
            }
            actual.Estatus = pago.Estatus; actual.Comentario = pago.Comentario;
            actual.IdUsuarioRevision = pago.IdUsuarioRevision; actual.FechaRevision = pago.FechaRevision;
            await _context.SaveChangesAsync(); if (tx != null) await tx.CommitAsync();
        }

        private async Task<List<Comision>> BloquearAsync(IEnumerable<long> ids)
        {
            var result = new List<Comision>();
            foreach (var id in ids.Distinct().OrderBy(x => x))
            {
                var c = _context.Database.IsRelational()
                    ? await _context.Comisiones.FromSqlInterpolated($"SELECT * FROM comisiones WHERE \"Id\" = {id} FOR UPDATE").AsTracking().SingleOrDefaultAsync()
                    : await _context.Comisiones.AsTracking().SingleOrDefaultAsync(x => x.Id == id);
                if (c == null) throw new PedidoOperacionException("409", "No se encontró una comisión del pago.");
                result.Add(c);
            }
            return result;
        }

        public async Task EliminarDetallesAsync(IEnumerable<PagoComisionDetalle> detalles)
        {
            _context.PagosComisionesDetalle.RemoveRange(detalles);
            await _context.SaveChangesAsync();
        }

        public async Task MarcarComisionesComoPagadasAsync(IEnumerable<long> comisionIds, DateTime fechaPago)
        {
            var idList = comisionIds.ToList();
            var comisiones = await _context.Comisiones
                .Where(x => idList.Contains(x.Id))
                .ToListAsync();

            foreach (var c in comisiones)
            {
                c.Estatus = (int)EstatusComision.Pagada;
                c.FechaPago = fechaPago;
            }

            await _context.SaveChangesAsync();
        }
    }
}
