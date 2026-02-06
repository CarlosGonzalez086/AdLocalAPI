using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.TipoComercio;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class TipoComercioRepository : ITipoComercioRepository
    {
        private readonly AppDbContext _context;

        public TipoComercioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TipoComercio?> GetById(long id)
        {
            return await _context.TipoComercio
                .Where(c => c.Activo)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ApiResponse<PagedResponse<TipoComercioDto>>> GetAllPagedAsync(
                   int page = 1,
                   int pageSize = 10,
                   string orderBy = "recent",
                   string search = ""
               )
        {
            var query = _context.TipoComercio.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.Nombre.Contains(search));

            query = orderBy.ToLower() switch
            {
                "az" => query.OrderBy(x => x.Nombre),
                "za" => query.OrderByDescending(x => x.Nombre),
                "recent" => query.OrderByDescending(x => x.Id),
                "old" => query.OrderBy(x => x.Id),
                _ => query.OrderByDescending(x => x.Id)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new TipoComercioDto
                {
                    Id = x.Id,
                    Nombre = x.Nombre,
                    Descripcion = x.Descripcion,
                    Activo = x.Activo
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<TipoComercioDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                Items = items
            };

            return ApiResponse<PagedResponse<TipoComercioDto>>.Success(pagedResponse, "Listado de tipos de comercio");
        }

        public async Task<TipoComercio> Create(TipoComercio entity)
        {
            await _context.TipoComercio.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task Update(TipoComercio entity)
        {
            _context.TipoComercio.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(TipoComercio entity)
        {
            _context.TipoComercio.Remove(entity);
            await _context.SaveChangesAsync();
        }
        public async Task<List<TipoComercio>> GetAllForSelect()
        {
            return await _context.TipoComercio
                .Where(t => t.Activo)
                .Select(t => new TipoComercio
                {
                    Id = t.Id,
                    Nombre = t.Nombre
                })
                .ToListAsync();
        }

    }
}
