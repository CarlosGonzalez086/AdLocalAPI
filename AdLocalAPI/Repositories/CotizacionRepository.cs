using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class CotizacionRepository : ICotizacionRepository
    {
        private readonly AppDbContext _context;

        public CotizacionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Cotizacion> CrearAsync(Cotizacion cotizacion, CancellationToken cancellationToken = default)
        {
            _context.Cotizaciones.Add(cotizacion);
            await _context.SaveChangesAsync(cancellationToken);
            return cotizacion;
        }

        public async Task<Cotizacion?> ObtenerPorUuidAsync(Guid uuid, CancellationToken cancellationToken = default)
        {
            return await _context.Cotizaciones
                .FirstOrDefaultAsync(x => x.Uuid == uuid, cancellationToken);
        }

        public async Task<Cotizacion?> ObtenerPorUuidYUsuarioAsync(Guid uuid, long idUsuario, CancellationToken cancellationToken = default)
        {
            return await _context.Cotizaciones
                .FirstOrDefaultAsync(x => x.Uuid == uuid && x.IdUsuario == idUsuario, cancellationToken);
        }

        public async Task<List<CotizacionItemDto>> ObtenerMiasAsync(long idUsuario, CancellationToken cancellationToken = default)
        {
            return await (from c in _context.Cotizaciones.AsNoTracking()
                          join p in _context.ProductosServicios on c.IdProductoServicio equals p.Id
                          join co in _context.Comercios on c.IdComercio equals co.Id
                          where c.IdUsuario == idUsuario
                          orderby c.FechaCreacion descending
                          select new CotizacionItemDto
                          {
                              Uuid = c.Uuid,
                              Servicio = p.Nombre,
                              Comercio = co.Nombre,
                              Solicitud = c.Solicitud,
                              Respuesta = c.Respuesta,
                              PrecioPropuesto = c.PrecioPropuesto,
                              Estado = c.Estado,
                              FechaCreacion = c.FechaCreacion
                          }).ToListAsync(cancellationToken);
        }

        public async Task ActualizarAsync(Cotizacion cotizacion, CancellationToken cancellationToken = default)
        {
            _context.Cotizaciones.Update(cotizacion);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<ProductosServicios?> ObtenerServicioParaCotizarAsync(Guid productoUuid, CancellationToken cancellationToken = default)
        {
            return await _context.ProductosServicios
                .FirstOrDefaultAsync(x => x.Uuid == productoUuid &&
                                          x.Modalidad == ModalidadProductoServicio.Cotizacion &&
                                          x.Activo &&
                                          x.Visible, cancellationToken);
        }
    }
}
