using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Org.BouncyCastle.Crypto.Digests;
using Supabase.Gotrue;


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
            double? lng,
            string? municipio
        )
        {
            IQueryable<Comercio> query = _context.Comercios
                .Where(c => c.Activo)
                .Include(c => c.Estado)      
                .Include(c => c.Municipio);

            query = query.Where(c => c.Municipio.MunicipioNombre == municipio);


            switch (tipo.ToLower())
            {
                case "destacados":
                    query = query
                        .Where(c =>
                            c.Activo &&
                            _context.Suscripcions.Any(s =>
                                s.UsuarioId == c.IdUsuario &&
                                s.Activa &&
                                !s.Eliminada &&
                                (s.Plan.Tipo == "PRO" || s.Plan.Tipo == "BUSINESS")
                            )
                        )
                        .OrderByDescending(c =>
                            _context.Suscripcions
                                .Where(s =>
                                    s.UsuarioId == c.IdUsuario &&
                                    s.Activa &&
                                    !s.Eliminada &&
                                    (s.Plan.Tipo == "PRO" || s.Plan.Tipo == "BUSINESS")
                                )
                                .Select(s => s.Plan.NivelVisibilidad)
                                .FirstOrDefault()
                        );
                    break;


                case "populares":
                    query = query
                        .OrderByDescending(c => c.CalificacionesComentarios.Any()
                            ? c.CalificacionesComentarios.Sum(cc => cc.Calificacion)
                              / (double)c.CalificacionesComentarios.Count()
                            : 0);
                    break;

                case "recientes":
                    query = query.OrderByDescending(c => c.FechaCreacion);
                    break;

                case "cercanos":
                    if (lat == null || lng == null)
                        throw new ArgumentException("Latitud y longitud son requeridas");

                    double maxKm = 5;
                    var userLocation = new Point(lng.Value, lat.Value) { SRID = 4326 };

                    query = query
                        .Where(c => c.Ubicacion != null &&
                                    c.Ubicacion.Distance(userLocation) <= maxKm / 111.32)
                        .OrderBy(c => c.Ubicacion.Distance(userLocation));
                    break;

                default:
                    query = query.OrderBy(c => c.Nombre);
                    break;
            }

            var result = await query
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
                    FechaCreacion = c.FechaCreacion,
                    Badge = _context.Suscripcions
                            .Where(s =>
                                s.UsuarioId == c.IdUsuario &&
                                s.Activa &&
                                !s.Eliminada &&
                                s.Plan.TieneBadge
                            )
                            .Select(s => s.Plan.BadgeTexto)
                            .FirstOrDefault() ?? "",
                    EstadoNombre = c.Estado!.EstadoNombre == null ? "" :c.Estado!.EstadoNombre,
                    MunicipioNombre = c.Municipio!.MunicipioNombre == null ? "" : c.Municipio!.MunicipioNombre,
                    PromedioCalificacion = c.CalificacionesComentarios.Any()
                                            ? c.CalificacionesComentarios.Sum(cc => cc.Calificacion)
                                              / (double)c.CalificacionesComentarios.Count()
                                            : 0
                })
                .ToListAsync();

            return result;
        }




        public async Task<Comercio> GetByIdAsync(long id)
        {

            try
            {
                return await _context.Comercios
                    .FirstOrDefaultAsync(c =>
                        c.Id == id &&
                        c.Activo
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task<Comercio> GetComercioByUser(long idUSer)
        {
            try
            {
                return await _context.Comercios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.IdUsuario == idUSer &&
                        c.Activo
                    );
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex);
                return null;
            }
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

        public async Task DeleteAsync(long id)
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
        public async Task<List<ComercioPublicDto>> GetByFiltros(int estadoId, int municipioId, string orden)
        {
            IQueryable<Comercio> query = _context.Comercios
                .Where(c => c.Activo)
                .Include(c => c.Estado)
                .Include(c => c.Municipio);


            if (estadoId != 0)
                query = query.Where(c => c.EstadoId == estadoId);

            if (municipioId != 0)
                query = query.Where(c => c.MunicipioId == municipioId);


            switch (orden.ToLower())
            {
                case "recientes":
                    query = query.OrderByDescending(c => c.FechaCreacion);
                    break;
                case "antiguos":
                    query = query.OrderBy(c => c.FechaCreacion);
                    break;
                case "populares":
                    query = query
                        .OrderByDescending(c => c.CalificacionesComentarios.Any()
                            ? c.CalificacionesComentarios.Sum(cc => cc.Calificacion)
                              / (double)c.CalificacionesComentarios.Count()
                            : 0);
                    break;
                default:
                    query = query.OrderBy(c => c.Nombre); 
                    break;
            }


            var result = await query.Select(c => new ComercioPublicDto
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
                FechaCreacion = c.FechaCreacion,
                EstadoNombre = c.Estado!.EstadoNombre,
                MunicipioNombre = c.Municipio!.MunicipioNombre,
                PromedioCalificacion = c.CalificacionesComentarios.Any()
                                            ? c.CalificacionesComentarios.Sum(cc => cc.Calificacion)
                                              / (double)c.CalificacionesComentarios.Count()
                                            : 0
            }).ToListAsync();

            return result;
        }
        public async Task<ApiResponse<PagedResponse<ComercioPublicDto>>> GetAllComerciosByUserPaged(
            long idUser,
            int page = 1,
            int pageSize = 10
        )
        {
            IQueryable<Comercio> query = _context.Comercios
                .Where(c => c.Activo && c.IdUsuario == idUser)
                .Include(c => c.Estado)
                .Include(c => c.Municipio)
                .Include(c => c.CalificacionesComentarios);

            // Badge del usuario (una sola vez)
            var badge = await _context.Suscripcions
                .Where(s =>
                    s.UsuarioId == idUser &&
                    s.Activa &&
                    !s.Eliminada &&
                    s.Plan.TieneBadge
                )
                .Select(s => s.Plan.BadgeTexto)
                .FirstOrDefaultAsync();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
                .OrderByDescending(c => c.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
                    FechaCreacion = c.FechaCreacion,
                    EstadoNombre = c.Estado!.EstadoNombre,
                    MunicipioNombre = c.Municipio!.MunicipioNombre,
                    Badge = badge,
                    PromedioCalificacion = c.CalificacionesComentarios.Any()
                        ? c.CalificacionesComentarios.Average(x => x.Calificacion)
                        : 0
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<ComercioPublicDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                Items = items
            };

            return ApiResponse<PagedResponse<ComercioPublicDto>>.Success(
                pagedResponse,
                "Listado de comercios obtenido correctamente"
            );
        }

        public async Task<List<Comercio>> GetAllComerciosByIdUsuario(long idUser)
        {
            return await _context.Comercios.Where((e)=>e.IdUsuario == idUser && e.Activo == true).ToListAsync();
        }

    }
}
