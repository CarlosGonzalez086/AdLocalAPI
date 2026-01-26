using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class CalificacionComentarioRepository
    {
        private readonly AppDbContext _context;

        public CalificacionComentarioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CalificacionComentario> CreateAsync(CalificacionComentario comentario)
        {
            _context.CalificacionComentario.Add(comentario);
            await _context.SaveChangesAsync();
            return comentario;
        }

        public async Task<object> GetAllAsync(long idComercio,
            int page = 1,
            int pageSize = 10,
            string orderBy = "desc"
        )
        {
            var query = _context.CalificacionComentario.AsQueryable();

            query = query.Where( c => c.IdComercio == idComercio );

            query = orderBy.ToLower() switch
            {
                "asc" => query.OrderBy(c => c.FechaCreacion),
                "desc" => query.OrderByDescending(c => c.FechaCreacion),
                _ => query.OrderByDescending(c => c.FechaCreacion)
            };

            var totalRecords = await query.CountAsync();

            var comentarios = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new
            {
                totalRecords,
                page,
                pageSize,
                data = comentarios
            };
        }
        public async Task<IEnumerable<Models.CalificacionComentario>> GetCalificacionByComercioAsync(long idComercio) 
        {
            var query = _context.CalificacionComentario.AsQueryable();

            query = query.Where(c => c.IdComercio == idComercio);
            return query;
        }
    }
}
