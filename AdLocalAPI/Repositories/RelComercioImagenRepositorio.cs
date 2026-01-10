using AdLocalAPI.Data;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using Supabase.Interfaces;

namespace AdLocalAPI.Repositories
{
    public class RelComercioImagenRepositorio : IRelComercioImagenRepositorio
    {
        private readonly AppDbContext _context;
        private readonly Supabase.Client _supabaseClient;
        private readonly IWebHostEnvironment _env;

        public RelComercioImagenRepositorio(AppDbContext context, Supabase.Client supabaseClient, IWebHostEnvironment env)
        {
            _context = context;
            _supabaseClient = supabaseClient;
            _env = env;
        }

        public async Task<List<RelComercioImagen>> ObtenerPorComercio(long idComercio)
        {
            return await _context.RelComercioImagen
                .Where(x => x.IdComercio == idComercio)
                .OrderByDescending(x => x.FechaCreacion)
                .ToListAsync();
        }

        public async Task<RelComercioImagen> Crear(long idComercio, string fotoUrl)
        {
            var entidad = new RelComercioImagen
            {
                IdComercio = idComercio,
                FotoUrl = fotoUrl,
                FechaCreacion = DateTime.UtcNow
            };

            _context.RelComercioImagen.Add(entidad);
            await _context.SaveChangesAsync();

            return entidad;
        }

        public async Task<bool> Editar(long idComercio, string fotoUrlActual, string nuevaFotoUrl)
        {
            var imagen = await _context.RelComercioImagen
                .FirstOrDefaultAsync(x =>
                    x.IdComercio == idComercio &&
                    x.FotoUrl == fotoUrlActual);

            if (imagen == null)
                return false;

            imagen.FotoUrl = nuevaFotoUrl;
            imagen.FechaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Eliminar(long idComercio, string fotoUrl)
        {
            var imagen = await _context.RelComercioImagen
                .FirstOrDefaultAsync(x =>
                    x.IdComercio == idComercio &&
                    x.FotoUrl == fotoUrl);

            if (imagen == null)
                return false;

            _context.RelComercioImagen.Remove(imagen);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<string> UploadToSupabaseAsync(byte[] imageBytes, int userId, string contentType = "image/png")
        {
            string envPrefix = _env.IsProduction() ? "prod" : "local";
            string fileName = $"{envPrefix}_RelComercioImagen{userId}_{DateTime.UtcNow.Ticks}.png";
            var bucket = _supabaseClient.Storage.From("RelComercioImagen");

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

                var path = uri.AbsolutePath.Split("/RelComercioImagen/").Last();

                var bucket = _supabaseClient.Storage.From("RelComercioImagen");

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
