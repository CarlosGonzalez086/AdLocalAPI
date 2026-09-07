using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IComisionRepository
    {
        Task<bool> ExisteOperacionAsync(int tipoOperacion, long idReferencia);
        Task AgregarAsync(Comision comision);
        Task<List<Pedido>> ObtenerPedidosPendientesConciliacionAsync(int tipoOperacion);
        Task<List<ComisionDiaDto>> ObtenerComisionesPorDiaAsync(DateTime inicioSemana);
        Task<decimal> ObtenerComisionesMesAsync(DateTime inicioMes);
        Task<decimal> ObtenerPendienteCobroAsync();
        Task<decimal> ObtenerCobradoMesAsync(DateTime inicioMes);
        Task<List<ComisionComercioResumenDto>> ObtenerResumenPorComercioAsync(DateTime desde);
        Task<(int total, List<ComisionMovimientoDto> items)> ObtenerMovimientosPaginadosAsync(int page, int pageSize, long? comercioId, int? estatus);
        Task<List<Comision>> ObtenerPendientesParaLiquidacionAsync(long comercioId, DateTime desde);
        Task ActualizarComisionesAsync(List<Comision> comisiones);
    }
}
