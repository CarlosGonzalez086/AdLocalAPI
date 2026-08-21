using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Services
{
    public class ComisionService : IComisionService
    {
        private readonly AppDbContext _context;
        public ComisionService(AppDbContext context) => _context = context;

        public async Task RegistrarVentaAsync(Pedido pedido)
        {
            if (pedido.EstadoPago != EstadoPagoPedido.Pagado || pedido.MontoComision <= 0) return;
            var tipo = (int)TipoOperacionComision.Venta;
            if (await _context.Comisiones.AnyAsync(x => x.TipoOperacion == tipo && x.IdReferencia == pedido.Id)) return;
            _context.Comisiones.Add(new Comision
            {
                Uuid = Guid.NewGuid(), IdComercio = pedido.IdComercio, TipoOperacion = tipo,
                IdReferencia = pedido.Id, MontoOperacion = pedido.Total,
                PorcentajeComision = pedido.PorcentajeComision, MontoComision = pedido.MontoComision,
                Estatus = (int)EstatusComision.Pendiente,
                Observaciones = $"Venta {pedido.NumeroPedido}. Comisión fija: {pedido.ComisionFija:C2}.",
                FechaCreacion = DateTime.UtcNow, Activo = true
            });
            await _context.SaveChangesAsync();
        }

        private async Task ConciliarAsync()
        {
            var tipo = (int)TipoOperacionComision.Venta;
            var pedidos = await _context.Pedidos.AsNoTracking()
                .Where(p => p.EstadoPago == EstadoPagoPedido.Pagado && p.MontoComision > 0 &&
                    !_context.Comisiones.Any(c => c.TipoOperacion == tipo && c.IdReferencia == p.Id)).ToListAsync();
            foreach (var pedido in pedidos) await RegistrarVentaAsync(pedido);
        }

        public async Task<ApiResponse<ComisionesDashboardDto>> ObtenerDashboardAsync()
        {
            await ConciliarAsync();
            var hoy = DateTime.UtcNow.Date;
            var inicioSemana = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var activas = _context.Comisiones.AsNoTracking().Where(x => x.Activo && x.Estatus != (int)EstatusComision.Cancelada);
            var datos = await activas.Where(x => x.FechaCreacion >= inicioSemana).GroupBy(x => x.FechaCreacion.Date)
                .Select(g => new { Fecha = g.Key, Monto = g.Sum(x => x.MontoComision) }).ToListAsync();
            var dias = Enumerable.Range(0, 7).Select(i => inicioSemana.AddDays(i)).Select(fecha => new ComisionDiaDto
            {
                Fecha = fecha, Dia = fecha.ToString("ddd", new System.Globalization.CultureInfo("es-MX")),
                Monto = datos.FirstOrDefault(x => x.Fecha == fecha)?.Monto ?? 0
            }).ToList();
            return ApiResponse<ComisionesDashboardDto>.Success(new ComisionesDashboardDto
            {
                ComisionesSemana = dias.Sum(x => x.Monto),
                ComisionesMes = await activas.Where(x => x.FechaCreacion >= inicioMes).SumAsync(x => (decimal?)x.MontoComision) ?? 0,
                PendienteCobro = await activas.Where(x => x.Estatus == (int)EstatusComision.Pendiente).SumAsync(x => (decimal?)x.MontoComision) ?? 0,
                CobradoMes = await activas.Where(x => x.Estatus == (int)EstatusComision.Pagada && x.FechaPago >= inicioMes).SumAsync(x => (decimal?)x.MontoComision) ?? 0,
                Semana = dias
            });
        }

        public async Task<ApiResponse<List<ComisionComercioResumenDto>>> ObtenerResumenAsync(string periodo)
        {
            await ConciliarAsync();
            var desde = ObtenerDesde(periodo);
            var query = from c in _context.Comisiones.AsNoTracking()
                        join p in _context.Pedidos.AsNoTracking() on c.IdReferencia equals p.Id
                        join comercio in _context.Comercios.AsNoTracking() on c.IdComercio equals comercio.Id
                        where c.Activo && c.TipoOperacion == (int)TipoOperacionComision.Venta && c.FechaCreacion >= desde
                        group new { c, p, comercio } by new { comercio.Id, comercio.Uuid, comercio.Nombre } into g
                        orderby g.Sum(x => x.c.Estatus == (int)EstatusComision.Pendiente ? x.c.MontoComision : 0) descending
                        select new ComisionComercioResumenDto
                        {
                            ComercioId = g.Key.Id, ComercioUuid = g.Key.Uuid, Comercio = g.Key.Nombre,
                            Ventas = g.Count(), VentasMonto = g.Sum(x => x.c.MontoOperacion), ComisionGenerada = g.Sum(x => x.c.MontoComision),
                            PendientePago = g.Sum(x => x.c.Estatus == (int)EstatusComision.Pendiente ? x.c.MontoComision : 0),
                            PendienteEfectivo = g.Sum(x => x.c.Estatus == (int)EstatusComision.Pendiente && x.p.MetodoPago == MetodoPagoPedido.Efectivo ? x.c.MontoComision : 0),
                            PendienteTransferencia = g.Sum(x => x.c.Estatus == (int)EstatusComision.Pendiente && x.p.MetodoPago == MetodoPagoPedido.Transferencia ? x.c.MontoComision : 0),
                            UltimaVenta = g.Max(x => (DateTime?)x.c.FechaCreacion)
                        };
            return ApiResponse<List<ComisionComercioResumenDto>>.Success(await query.ToListAsync());
        }

        public async Task<ApiResponse<PagedResponse<ComisionMovimientoDto>>> ObtenerMovimientosAsync(int page, int pageSize, long? comercioId, int? estatus)
        {
            await ConciliarAsync(); page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
            var query = from c in _context.Comisiones.AsNoTracking()
                        join p in _context.Pedidos.AsNoTracking() on c.IdReferencia equals p.Id
                        join comercio in _context.Comercios.AsNoTracking() on c.IdComercio equals comercio.Id
                        where c.Activo && (!comercioId.HasValue || c.IdComercio == comercioId) && (!estatus.HasValue || c.Estatus == estatus)
                        orderby c.FechaCreacion descending
                        select new ComisionMovimientoDto { Uuid = c.Uuid, Comercio = comercio.Nombre, PedidoUuid = p.Uuid, NumeroPedido = p.NumeroPedido,
                            MetodoPago = p.MetodoPago == MetodoPagoPedido.Efectivo ? "Efectivo" : "Transferencia", MontoVenta = c.MontoOperacion,
                            Porcentaje = c.PorcentajeComision, ComisionFija = p.ComisionFija, MontoComision = c.MontoComision,
                            Estatus = c.Estatus, Fecha = c.FechaCreacion, FechaPago = c.FechaPago };
            var total = await query.CountAsync();
            return ApiResponse<PagedResponse<ComisionMovimientoDto>>.Success(new PagedResponse<ComisionMovimientoDto>
            { Page = page, PageSize = pageSize, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize), Items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync() });
        }

        public async Task<ApiResponse<object>> LiquidarAsync(long comercioId, string periodo)
        {
            var desde = ObtenerDesde(periodo);
            var pendientes = await _context.Comisiones.Where(x => x.IdComercio == comercioId && x.Activo &&
                x.Estatus == (int)EstatusComision.Pendiente && x.FechaCreacion >= desde &&
                !_context.PagosComisionesDetalle.Any(d => d.IdComision == x.Id)).ToListAsync();
            if (pendientes.Count == 0) return ApiResponse<object>.Error("404", "No hay comisiones pendientes en el periodo.");
            var fecha = DateTime.UtcNow;
            foreach (var item in pendientes) { item.Estatus = (int)EstatusComision.Pagada; item.FechaPago = fecha; }
            await _context.SaveChangesAsync();
            return ApiResponse<object>.Success(new { cantidad = pendientes.Count, total = pendientes.Sum(x => x.MontoComision) }, "Comisiones marcadas como pagadas.");
        }

        private static DateTime ObtenerDesde(string periodo)
        {
            var hoy = DateTime.UtcNow.Date;
            return periodo.Equals("mes", StringComparison.OrdinalIgnoreCase)
                ? new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                : hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
        }
    }
}
