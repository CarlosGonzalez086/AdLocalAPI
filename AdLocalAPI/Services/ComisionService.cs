using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Services
{
    public class ComisionService : IComisionService
    {
        private readonly IComisionRepository _comisionRepository;

        public ComisionService(IComisionRepository comisionRepository)
        {
            _comisionRepository = comisionRepository;
        }

        public async Task RegistrarVentaAsync(Pedido pedido)
        {
            if (pedido.EstadoPago != EstadoPagoPedido.Pagado || pedido.MontoComision <= 0) return;
            var tipo = (int)TipoOperacionComision.Venta;
            if (await _comisionRepository.ExisteOperacionAsync(tipo, pedido.Id)) return;

            await _comisionRepository.AgregarAsync(new Comision
            {
                Uuid = Guid.NewGuid(),
                IdComercio = pedido.IdComercio,
                TipoOperacion = tipo,
                IdReferencia = pedido.Id,
                MontoOperacion = pedido.Total,
                PorcentajeComision = pedido.PorcentajeComision,
                MontoComision = pedido.MontoComision,
                Estatus = (int)EstatusComision.Pendiente,
                Observaciones = $"Venta {pedido.NumeroPedido}. Comisión fija: {pedido.ComisionFija:C2}.",
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            });
        }

        private async Task ConciliarAsync()
        {
            var tipo = (int)TipoOperacionComision.Venta;
            var pedidos = await _comisionRepository.ObtenerPedidosPendientesConciliacionAsync(tipo);
            foreach (var pedido in pedidos)
            {
                await RegistrarVentaAsync(pedido);
            }
        }

        public async Task<ApiResponse<ComisionesDashboardDto>> ObtenerDashboardAsync()
        {
            await ConciliarAsync();
            var hoy = DateTime.UtcNow.Date;
            var inicioSemana = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var dias = await _comisionRepository.ObtenerComisionesPorDiaAsync(inicioSemana);
            var comisionesMes = await _comisionRepository.ObtenerComisionesMesAsync(inicioMes);
            var pendienteCobro = await _comisionRepository.ObtenerPendienteCobroAsync();
            var cobradoMes = await _comisionRepository.ObtenerCobradoMesAsync(inicioMes);

            return ApiResponse<ComisionesDashboardDto>.Success(new ComisionesDashboardDto
            {
                ComisionesSemana = dias.Sum(x => x.Monto),
                ComisionesMes = comisionesMes,
                PendienteCobro = pendienteCobro,
                CobradoMes = cobradoMes,
                Semana = dias
            });
        }

        public async Task<ApiResponse<List<ComisionComercioResumenDto>>> ObtenerResumenAsync(string periodo)
        {
            await ConciliarAsync();
            var desde = ObtenerDesde(periodo);
            var resumen = await _comisionRepository.ObtenerResumenPorComercioAsync(desde);
            return ApiResponse<List<ComisionComercioResumenDto>>.Success(resumen);
        }

        public async Task<ApiResponse<PagedResponse<ComisionMovimientoDto>>> ObtenerMovimientosAsync(int page, int pageSize, long? comercioId, int? estatus)
        {
            await ConciliarAsync();
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (total, items) = await _comisionRepository.ObtenerMovimientosPaginadosAsync(page, pageSize, comercioId, estatus);

            return ApiResponse<PagedResponse<ComisionMovimientoDto>>.Success(new PagedResponse<ComisionMovimientoDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Items = items
            });
        }

        public async Task<ApiResponse<object>> LiquidarAsync(long comercioId, string periodo)
        {
            var desde = ObtenerDesde(periodo);
            var pendientes = await _comisionRepository.ObtenerPendientesParaLiquidacionAsync(comercioId, desde);
            if (pendientes.Count == 0)
            {
                return ApiResponse<object>.Error("404", "No hay comisiones pendientes en el periodo.");
            }

            var fecha = DateTime.UtcNow;
            foreach (var item in pendientes)
            {
                item.Estatus = (int)EstatusComision.Pagada;
                item.FechaPago = fecha;
            }

            await _comisionRepository.ActualizarComisionesAsync(pendientes);

            return ApiResponse<object>.Success(new
            {
                cantidad = pendientes.Count,
                total = pendientes.Sum(x => x.MontoComision)
            }, "Comisiones marcadas como pagadas.");
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
