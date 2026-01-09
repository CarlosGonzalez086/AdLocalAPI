using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class ProductosServiciosRepository : IProductosServiciosRepository
    {
        private readonly AppDbContext _context;

        public ProductosServiciosRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProductosServicios> CreateAsync(ProductosServicios entity)
        {
            _context.ProductosServicios.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<IEnumerable<ProductosServicios>> GetAllAsync(long idComercio)
        {
            return await _context.ProductosServicios
                .Where(x => x.IdComercio == idComercio).ToListAsync();                
        }

        public async Task<ProductosServicios?> GetByIdAsync(long id, long idComercio, long idUser)
        {
            return await _context.ProductosServicios
                .FirstOrDefaultAsync(x => x.Id == id && x.IdComercio == idComercio && x.IdUsuario == idUser);
        }

        public async Task UpdateAsync(ProductosServicios entity)
        {
            _context.ProductosServicios.Update(entity);
            await _context.SaveChangesAsync();
        }
        public async Task<ApiResponse<PagedResponse<ProductosServiciosDto>>> GetAllPagedAsync(
                   long idUser, long idComercio, int page = 1, int pageSize = 10, string orderBy = "recent", string search = "")
        {
            var query = _context.ProductosServicios
                .Where(x => x.IdComercio == idComercio && x.IdUsuario == idUser);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.Nombre.Contains(search));

            query = orderBy.ToLower() switch
            {
                "name" => query.OrderBy(x => x.Nombre),
                _ => query.OrderByDescending(x => x.Id)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProductosServiciosDto
                {
                    Id = x.Id,
                    Nombre = x.Nombre,
                    Descripcion = x.Descripcion,
                    Tipo = (int)x.Tipo,
                    Precio = x.Precio,
                    Stock = x.Stock,
                    Activo = x.Activo
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<ProductosServiciosDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                Items = items
            };

            return ApiResponse<PagedResponse<ProductosServiciosDto>>.Success(pagedResponse, "Listado de productos/servicios");
        }
    }
}
