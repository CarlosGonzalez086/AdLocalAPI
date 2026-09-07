using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Services
{
    public interface ICotizacionService
    {
        Task<ApiResponse<object>> CrearCotizacionAsync(long idUsuario, CrearCotizacionDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<List<CotizacionItemDto>>> ObtenerMiasAsync(long idUsuario, CancellationToken cancellationToken = default);
        Task<ApiResponse<object>> CancelarCotizacionAsync(long idUsuario, Guid uuid, CancellationToken cancellationToken = default);
    }
}
