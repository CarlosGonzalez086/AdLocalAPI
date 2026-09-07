using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class ComisionRepository : IComisionRepository
    {
        private readonly AppDbContext _context;

        public ComisionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExisteOperacionAsync(int tipoOperacion, long idReferencia)
        {
            return await _context.Comisiones.AnyAsync(x => x.TipoOperacion == tipoOperacion && x.IdReferencia == idReferencia);
        }

        public async Task AgregarAsync(Comision comision)
        {
            _context.Comisiones.Add(comision);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Pedido>> ObtenerPedidosPendientesConciliacionAsync(int tipoOperacion)
        {
            return await _context.Pedidos.AsNoTracking()
                .Where(p => p.EstadoPago == EstadoPagoPedido.Pagado && p.MontoComision > 0 &&
                    !_context.Comisiones.Any(c => c.TipoOperacion == tipoOperacion && c.IdReferencia == p.Id))
                .ToListAsync();
        }

        public async Task<List<ComisionDiaDto>> ObtenerComisionesPorDiaAsync(DateTime inicioSemana)
        {
            var activas = _context.Comisiones.AsNoTracking().Where(x => x.Activo && x.Estatus != (int)EstatusComision.Cancelada);
            var datos = await activas.Where(x => x.FechaCreacion >= inicioSemana)
                .GroupBy(x => x.FechaCreacion.Date)
                .Select(g => new { Fecha = g.Key, Monto = g.Sum(x => x.MontoComision) })
                .ToListAsync();

            var dias = Enumerable.Range(0, 7)
                .Select(i => inicioSemana.AddDays(i))
                .Select(fecha => new ComisionDiaDto
                {
                    Fecha = fecha,
                    Dia = fecha.ToString("ddd", new System.Globalization.CultureInfo("es-MX")),
                    Monto = datos.FirstOrDefault(x => x.Fecha == fecha)?.Monto ?? 0
                }).ToList();

            return dias;
        }

        public async Task<decimal> ObtenerComisionesMesAsync(DateTime inicioMes)
        {
            var activas = _context.Comisiones.AsNoTracking().Where(x => x.Activo && x.Estatus != (int)EstatusComision.Cancelada);
            return await activas.Where(x => x.FechaCreacion >= inicioMes).SumAsync(x => (decimal?)x.MontoComision) ?? 0;
        }

        public async Task<decimal> ObtenerPendienteCobroAsync()
        {
            var activas = _context.Comisiones.AsNoTracking().Where(x => x.Activo && x.Estatus != (int)EstatusComision.Cancelada);
            return await activas.Where(x => x.Estatus == (int)EstatusComision.Pendiente).SumAsync(x => (decimal?)x.MontoComision) ?? 0;
        }

        public async Task<decimal> ObtenerCobradoMesAsync(DateTime inicioMes)
        {
            var activas = _context.Comisiones.AsNoTracking().Where(x => x.Activo && x.Estatus != (int)EstatusComision.Cancelada);
            return await activas.Where(x => x.Estatus == (int)EstatusComision.Pagada && x.FechaPago >= inicioMes).SumAsync(x => (decimal?)x.MontoComision) ?? 0;
        }

        public async Task<List<ComisionComercioResumenDto>> ObtenerResumenPorComercioAsync(DateTime desde)
        {
            var query = from c in _context.Comisiones.AsNoTracking()
                        join p in _context.Pedidos.AsNoTracking() on c.IdReferencia equals p.Id
                        join comercio in _context.Comercios.AsNoTracking() on c.IdComercio equals comercio.Id
                        where c.Activo && c.TipoOperacion == (int)TipoOperacionComision.Venta && c.FechaCreacion >= desde
                        group new { c, p, comercio } by new { comercio.Id, comercio.Uuid, comercio.Nombre } into g
                        orderby g.Sum(x => x.c.Estatus == (int)EstatusComision.Pendiente ? x.c.MontoComision : 0) descending
                        select new ComisionComercioResumenDto
                        {
                            ComercioId = g.Key.Id,
                            ComercioUuid = g.Key.Uuid,
                            Comercio = g.Key.Nombre,
                            Ventas = g.Count(),
                            VentasMonto = g.Sum(x => x.c.MontoOperacion),
                            ComisionGenerada = g.Sum(x => x.c.MontoComision),
                            PendientePago = g.Sum(x => x.c.Estatus == (int)EstatusComision.Pendiente ? x.c.MontoComision : 0),
                            PendienteEfectivo = g.Sum(x => x.c.Estatus == (int)EstatusComision.Pendiente && x.p.MetodoPago == MetodoPagoPedido.Efectivo ? x.c.MontoComision : 0),
                            PendienteTransferencia = g.Sum(x => x.c.Estatus == (int)EstatusComision.Pendiente && x.p.MetodoPago == MetodoPagoPedido.Transferencia ? x.c.MontoComision : 0),
                            UltimaVenta = g.Max(x => (DateTime?)x.c.FechaCreacion)
                        };

            return await query.ToListAsync();
        }

        public async Task<(int total, List<ComisionMovimientoDto> items)> ObtenerMovimientosPaginadosAsync(int page, int pageSize, long? comercioId, int? estatus)
        {
            var query = from c in _context.Comisiones.AsNoTracking()
                        join p in _context.Pedidos.AsNoTracking() on c.IdReferencia equals p.Id
                        join comercio in _context.Comercios.AsNoTracking() on c.IdComercio equals comercio.Id
                        where c.Activo && (!comercioId.HasValue || c.IdComercio == comercioId) && (!estatus.HasValue || c.Estatus == estatus)
                        orderby c.FechaCreacion descending
                        select new ComisionMovimientoDto
                        {
                            Uuid = c.Uuid,
                            Comercio = comercio.Nombre,
                            PedidoUuid = p.Uuid,
                            NumeroPedido = p.NumeroPedido,
                            MetodoPago = p.MetodoPago == MetodoPagoPedido.Efectivo ? "Efectivo" : "Transferencia",
                            MontoVenta = c.MontoOperacion,
                            Porcentaje = c.PorcentajeComision,
                            ComisionFija = p.ComisionFija,
                            MontoComision = c.MontoComision,
                            Estatus = c.Estatus,
                            Fecha = c.FechaCreacion,
                            FechaPago = c.FechaPago
                        };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (total, items);
        }

        public async Task<List<Comision>> ObtenerPendientesParaLiquidacionAsync(long comercioId, DateTime desde)
        {
            return await _context.Comisiones
                .Where(x => x.IdComercio == comercioId && x.Activo &&
                    x.Estatus == (int)EstatusComision.Pendiente && x.FechaCreacion >= desde &&
                    !_context.PagosComisionesDetalle.Any(d => d.IdComision == x.Id))
                .ToListAsync();
        }

        public async Task ActualizarComisionesAsync(List<Comision> comisiones)
        {
            _context.Comisiones.UpdateRange(comisiones);
            await _context.SaveChangesAsync();
        }
    }
}
