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
                .Where(x => x.IdComercio == comercioId && x.Activo && x.Estatus == 1 && x.FechaCreacion >= desde && !_context.PagosComisionesDetalle.Any(d => d.IdComision == x.Id))
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
                .Where(x => x.IdComercio == comercioId && x.Activo && x.Estatus == 1 && x.FechaCreacion >= desde && !_context.PagosComisionesDetalle.Any(d => d.IdComision == x.Id))
                .ToListAsync();
        }

        public async Task<PagoComision> CrearPagoAsync(PagoComision pago)
        {
            _context.PagosComisiones.Add(pago);
            await _context.SaveChangesAsync();
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
            _context.PagosComisiones.Update(pago);
            await _context.SaveChangesAsync();
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
