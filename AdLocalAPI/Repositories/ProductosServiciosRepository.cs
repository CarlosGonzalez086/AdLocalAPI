using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using Supabase.Interfaces;

namespace AdLocalAPI.Repositories
{
    public class ProductosServiciosRepository : IProductosServiciosRepository
    {
        private readonly AppDbContext _context;
        private readonly Supabase.Client _supabaseClient;
        private readonly IWebHostEnvironment _env;

        public ProductosServiciosRepository(AppDbContext context, Supabase.Client supabaseClient, IWebHostEnvironment env)
        {
            _context = context;
            _supabaseClient = supabaseClient;
            _env = env;
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
                .Select(x => new ProductosServiciosDto
                {
                    Id = x.Id,
                    Nombre = x.Nombre,
                    Descripcion = x.Descripcion,
                    Tipo = (int)x.Tipo,
                    Precio = x.Precio,
                    Stock = (int)x.Stock,
                    Activo = x.Activo,
                    ImagenBase64 = x.LogoUrl,
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
        public async Task<string> UploadToSupabaseAsync(
            byte[] imageBytes,
            int userId,
            string contentType = "image/png")
        {
            try
            {
                string envPrefix = _env.IsProduction() ? "prod" : "local";
                string fileName = $"{envPrefix}_ProductosServicios_{userId}_{DateTime.UtcNow.Ticks}.png";

                var bucket = _supabaseClient.Storage.From("ProductosServicios");

                var options = new Supabase.Storage.FileOptions
                {
                    ContentType = contentType
                };

                await bucket.Upload(imageBytes, fileName, options);

                return bucket.GetPublicUrl(fileName);
            }
            catch (Exception ex)
            {
                // aquí puedes usar ILogger si lo tienes
                // _logger.LogError(ex, "Error al subir imagen a Supabase");

                throw new Exception("Error al subir la imagen a Supabase.", ex);
            }
        }

        public async Task<bool> DeleteFromSupabaseByUrlAsync(string publicUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(publicUrl))
                    return false;

                var uri = new Uri(publicUrl);

                var path = uri.AbsolutePath.Split("/ProductosServicios/").Last();

                var bucket = _supabaseClient.Storage.From("ProductosServicios");

                await bucket.Remove(path);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
