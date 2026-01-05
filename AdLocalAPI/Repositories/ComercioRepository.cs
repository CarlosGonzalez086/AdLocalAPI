using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using Supabase.Interfaces;

namespace AdLocalAPI.Repositories
{
    public class ComercioRepository
    {
        private readonly AppDbContext _context;
        private readonly Supabase.Client _supabaseClient;
        private readonly IWebHostEnvironment _env;

        public ComercioRepository(AppDbContext context, Supabase.Client supabaseClient, IWebHostEnvironment env)
        {
            _context = context;
            _supabaseClient = supabaseClient;
            _env = env;
        }

        public async Task<List<Comercio>> GetAllAsync()
        {
            return await _context.Comercios.ToListAsync();
        }

        public async Task<Comercio> GetByIdAsync(int id)
        {
            return await _context.Comercios.FindAsync(id);
        }

        public async Task<Comercio> GetComercioByUser(long idUSer)
        {
            return await _context.Comercios
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdUsuario == idUSer && c.Activo);
        }

        public async Task<Comercio> CreateAsync(Comercio comercio)
        {
            _context.Comercios.Add(comercio);
            await _context.SaveChangesAsync();
            return comercio;
        }

        public async Task UpdateAsync(Comercio comercio)
        {
            _context.Comercios.Update(comercio);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var comercio = await _context.Comercios.FindAsync(id);
            if (comercio != null)
            {
                _context.Comercios.Remove(comercio);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<string> UploadToSupabaseAsync(byte[] imageBytes, int userId, string contentType = "image/png")
        {
            string envPrefix = _env.IsProduction() ? "prod" : "local";
            string fileName = $"{envPrefix}_LogoComercio{userId}_{DateTime.UtcNow.Ticks}.png";
            var bucket = _supabaseClient.Storage.From("LogoComercio");

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

                var path = uri.AbsolutePath.Split("/LogoComercio/").Last();

                var bucket = _supabaseClient.Storage.From("LogoComercio");

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
