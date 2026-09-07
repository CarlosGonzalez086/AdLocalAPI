using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Services
{
    public interface IPagoComisionService
    {
        Task<ApiResponse<EstadoComisionesComercioDto>> ObtenerEstadoAsync(long userId, string userRole, long comercioId);
        Task<ApiResponse<PagoComisionListadoDto>> CrearPagoAsync(long userId, string userRole, CrearPagoComisionDto dto);
        Task<ApiResponse<List<PagoComisionListadoDto>>> ListarAdminAsync(int? estatus = null);
        Task<ApiResponse<object>> RevisarPagoAsync(long userIdRevision, Guid uuid, RevisarPagoComisionDto dto);
        Task<ComprobanteArchivoDto> ObtenerComprobanteAsync(long userId, string userRole, Guid uuid);
    }
}
