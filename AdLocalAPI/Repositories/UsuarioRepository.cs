using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class UsuarioRepository
    {
        private readonly AppDbContext _context;
        private readonly Supabase.Client _supabaseClient;
        private readonly IWebHostEnvironment _env;

        public UsuarioRepository(AppDbContext context, Supabase.Client supabaseClient, IWebHostEnvironment env)
        {
            _context = context;
            _supabaseClient = supabaseClient;
            _env = env;
        }
        public async Task<bool> ExistePorCorreoAsync(string correo)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Email == correo);
        }

        public async Task<Usuario?> GetByCorreoAsync(string correo)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == correo);
        }

        public async Task<object> GetAllAsync(int page,
            int pageSize,
            string orderBy,
            string search)
        {
            var query = _context.Usuarios.AsQueryable();


            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.Nombre, $"%{search}%")
                );
            }

            query = query.Where(p => p.Rol == "Comercio");
            query = orderBy switch
            {
                "recent" => query.OrderByDescending(p => p.FechaCreacion),
                "old" => query.OrderBy(p => p.FechaCreacion),
                "az" => query.OrderBy(p => p.Nombre),
                "za" => query.OrderByDescending(p => p.Nombre),
                _ => query.OrderByDescending(p => p.FechaCreacion)
            };

            var totalRecords = await query.CountAsync();

            var plans = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new
            {
                totalRecords,
                page,
                pageSize,
                data = plans
            };
        }

        public async Task<Usuario> GetByIdAsync(int id)
        {
            return await _context.Usuarios.Include(u => u.Comercio)
                                          .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task UpdateAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<string> UploadToSupabaseAsync(byte[] imageBytes, int userId, string contentType = "image/png")
        {

            string envPrefix = _env.IsProduction() ? "prod" : "local";
            string fileName = $"{envPrefix}_Perfil_{userId}_{DateTime.UtcNow.Ticks}.png";
            var bucket = _supabaseClient.Storage.From("Perfil");

            var options = new Supabase.Storage.FileOptions
            {
                ContentType = contentType
            };


            await bucket.Upload(imageBytes, fileName, options);

            return bucket.GetPublicUrl(fileName);
        }
        public async Task<bool> DeleteFromSupabaseByUrlAsync(string publicUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(publicUrl))
                    return false;

                var uri = new Uri(publicUrl);

                var path = uri.AbsolutePath.Split("/Perfil/").Last();

                var bucket = _supabaseClient.Storage.From("Perfil");

                await bucket.Remove(path);

                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task UpdateUserPhotoUrlAsync(int userId, string url)
        {
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new Exception("Usuario no encontrado");

            user.FotoUrl = url;
            _context.Usuarios.Update(user);
            await _context.SaveChangesAsync();
        }

    }
}
