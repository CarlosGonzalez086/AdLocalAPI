using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface ICotizacionRepository
    {
        Task<Cotizacion> CrearAsync(Cotizacion cotizacion, CancellationToken cancellationToken = default);
        Task<Cotizacion?> ObtenerPorUuidAsync(Guid uuid, CancellationToken cancellationToken = default);
        Task<Cotizacion?> ObtenerPorUuidYUsuarioAsync(Guid uuid, long idUsuario, CancellationToken cancellationToken = default);
        Task<List<CotizacionItemDto>> ObtenerMiasAsync(long idUsuario, CancellationToken cancellationToken = default);
        Task ActualizarAsync(Cotizacion cotizacion, CancellationToken cancellationToken = default);
        Task<ProductosServicios?> ObtenerServicioParaCotizarAsync(Guid productoUuid, CancellationToken cancellationToken = default);
    }
}
