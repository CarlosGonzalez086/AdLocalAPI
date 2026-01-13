using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;


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

        public async Task<List<Comercio>> GetAllWitoutFilterAsync()
        {
            return await _context.Comercios.ToListAsync();
        }

        public async Task<List<ComercioPublicDto>> GetAllAsync(
            string tipo,
            double? lat,
            double? lng
        )
        {
            IQueryable<Comercio> query = _context.Comercios
                .Where(c => c.Activo);

            switch (tipo.ToLower())
            {
                case "recientes":
                    query = query.OrderByDescending(c => c.FechaCreacion);
                    break;

                case "cercanos":
                    if (lat == null || lng == null)
                        throw new ArgumentException("Latitud y longitud son requeridas");


                    double maxKm = 5;

                    var userLocation = new Point(lng.Value, lat.Value)
                    {
                        SRID = 4326
                    };

                    query = query
                        .Where(c => c.Ubicacion != null &&
                                    c.Ubicacion.Distance(userLocation) <= maxKm / 111.32)
                        .OrderBy(c => c.Ubicacion.Distance(userLocation));

                    break;

                default:
                    query = query.OrderBy(c => c.Nombre);
                    break;
            }

            return await query
                .Where(c => c.Ubicacion != null)
                .Select(c => new ComercioPublicDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Direccion = c.Direccion,
                    Telefono = c.Telefono,
                    Email = c.Email,
                    LogoUrl = c.LogoUrl,
                    Lat = c.Ubicacion!.Y,
                    Lng = c.Ubicacion!.X,

                    ColorPrimario = c.ColorPrimario,
                    ColorSecundario = c.ColorSecundario,
                    Activo = c.Activo,
                    FechaCreacion = c.FechaCreacion
                })
                .ToListAsync();
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
