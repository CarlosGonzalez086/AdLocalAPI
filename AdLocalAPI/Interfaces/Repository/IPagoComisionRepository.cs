using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IPagoComisionRepository
    {
        Task<Comercio?> ObtenerComercioPorIdAsync(long comercioId);
        Task<decimal> ObtenerSumaComisionesPendientesAsync(long comercioId, DateTime desde);
        Task<PagoComisionListadoDto?> ObtenerPagoEnRevisionPorComercioAsync(long comercioId);
        Task<bool> TienePagoEnRevisionAsync(long comercioId);
        Task<CuentaBancariaAdLocal?> ObtenerCuentaBancariaActivaPorUuidAsync(Guid uuid);
        Task<List<Comision>> ObtenerComisionesPendientesAsync(long comercioId, DateTime desde);
        Task<PagoComision> CrearPagoAsync(PagoComision pago);
        Task<List<PagoComisionListadoDto>> ListarAdminAsync(int? estatus = null);
        Task<PagoComision?> ObtenerPorUuidAsync(Guid uuid, bool incluirDetalles = false);
        Task ActualizarPagoAsync(PagoComision pago);
        Task EliminarDetallesAsync(IEnumerable<PagoComisionDetalle> detalles);
        Task MarcarComisionesComoPagadasAsync(IEnumerable<long> comisionIds, DateTime fechaPago);
    }
}
