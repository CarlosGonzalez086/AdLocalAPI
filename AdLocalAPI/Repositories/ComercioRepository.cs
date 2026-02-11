using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Org.BouncyCastle.Crypto.Digests;
using Supabase.Gotrue;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


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

        public async Task<(List<ComercioPublicDto> comercios, int total)> GetAllAsync(
            string tipo,
            double lat,
            double lng,
            string municipio,
            int page,
            int pageSize,
            string ip
        )
        {
            var hoy = DateTime.UtcNow.Date;
            double maxKm = 0;
            List<long> comerciosVisitadosPorIp = new();
            List<long> tiposVisitadosPorIp = new();
            try 
            {
                if (!string.IsNullOrEmpty(ip) && tipo == "sugeridos")
                {
                    comerciosVisitadosPorIp = await _context.ComercioVisitas
                        .Where(v => v.Ip == ip)
                        .OrderByDescending(v => v.FechaVisita)
                        .Select(v => v.ComercioId)
                        .Distinct()
                        .Take(20)
                        .ToListAsync();

                    if (comerciosVisitadosPorIp.Any())
                    {
                        tiposVisitadosPorIp = await _context.Comercios
                            .Where(c =>
                                comerciosVisitadosPorIp.Contains(c.Id) &&
                                c.TipoComercioId.HasValue
                            )
                            .Select(c => c.TipoComercioId!.Value)
                            .Distinct()
                            .ToListAsync();

                    }
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex);
            }
               




            if (lat == null || lng == null)
                throw new ArgumentException("Latitud y longitud son requeridas");

            var userLocation = new Point(lng, lat) { SRID = 4326 };

            var suscripcionesVigentes = _context.Suscripcions
                .Where(s =>
                    s.IsActive &&
                    !s.IsDeleted &&
                    s.CurrentPeriodEnd > hoy &&
                    (s.Plan.Tipo == "BASIC" || s.Plan.Tipo == "PRO" || s.Plan.Tipo == "BUSINESS")
                )
                .Select(s => new
                {
                    s.UsuarioId,
                    s.CurrentPeriodEnd,
                    s.Plan.MaxNegocios,
                    s.Plan.BadgeTexto,
                    s.Plan.NivelVisibilidad,
                    s.Plan.Tipo
                })
                .AsEnumerable()
                .GroupBy(s => s.UsuarioId)
                .Select(g => g.OrderByDescending(x => x.CurrentPeriodEnd).First())
                .ToList();

            IQueryable<Comercio> comerciosQuery = _context.Comercios
                .Where(c => c.Activo && c.Ubicacion != null)
                .Include(c => c.Estado)
                .Include(c => c.Municipio)
                .Include(c => c.CalificacionesComentarios);

            if (!string.IsNullOrEmpty(municipio))
            {
                comerciosQuery = comerciosQuery
                    .Where(c => c.Municipio.MunicipioNombre == municipio);
            }

            var query =
                from c in comerciosQuery.AsEnumerable()
                join s in suscripcionesVigentes
                    on c.IdUsuario equals s.UsuarioId into sus
                from suscripcion in sus.DefaultIfEmpty()
                select new
                {
                    Comercio = c,
                    Suscripcion = suscripcion
                };

            switch (tipo.ToLower())
            {
                case "destacados":
                    query = query
                        .Where(x => x.Suscripcion != null)
                        .OrderByDescending(x => x.Suscripcion.NivelVisibilidad);
                    break;

                case "populares":
                    query = query.OrderByDescending(x =>
                        x.Comercio.CalificacionesComentarios.Any()
                            ? x.Comercio.CalificacionesComentarios.Average(cc => cc.Calificacion)
                            : 0);
                    break;

                case "recientes":
                    query = query.OrderByDescending(x => x.Comercio.FechaCreacion);
                    break;

                case "cercanos":
                    maxKm = 5;
                    query = query
                        .Where(x => x.Comercio.Ubicacion.Distance(userLocation) <= maxKm / 111.32)
                        .OrderBy(x => x.Comercio.Ubicacion.Distance(userLocation));
                    break;
                case "sugeridos":
                    maxKm = 10;

                    query = query
                        .Where(x =>
                            x.Comercio.Ubicacion.Distance(userLocation) <= maxKm / 111.32
                        )
                        .OrderByDescending(x =>
                            (tiposVisitadosPorIp.Any() &&
                             tiposVisitadosPorIp.Contains((long)x.Comercio.TipoComercioId)) ? 3 : 0
                        )
                        .ThenByDescending(x =>
                            (comerciosVisitadosPorIp.Any() &&
                             comerciosVisitadosPorIp.Contains(x.Comercio.Id)) ? 2 : 0
                        )
                        .ThenByDescending(x =>
                            x.Comercio.CalificacionesComentarios.Any()
                                ? x.Comercio.CalificacionesComentarios.Average(cc => cc.Calificacion)
                                : 0
                        )
                        .ThenBy(x =>
                            x.Comercio.Ubicacion.Distance(userLocation)
                        );

                    break;



                default:
                    query = query.OrderBy(x => x.Comercio.Nombre);
                    break;
            }


            var lista = query
                .Select(x => new ComercioPublicDto
                {
                    Id = x.Comercio.Id,
                    Nombre = x.Comercio.Nombre,
                    Direccion = x.Comercio.Direccion,
                    Telefono = x.Comercio.Telefono,
                    Email = x.Comercio.Email,
                    LogoUrl = x.Comercio.LogoUrl,
                    Lat = x.Comercio.Ubicacion!.Y,
                    Lng = x.Comercio.Ubicacion!.X,
                    ColorPrimario = x.Comercio.ColorPrimario,
                    ColorSecundario = x.Comercio.ColorSecundario,
                    Badge = x.Suscripcion?.BadgeTexto ?? "",
                    PlanTipo = x.Suscripcion?.Tipo,
                    SuscripcionVigente = x.Suscripcion != null,
                    EstadoNombre = x.Comercio.Estado.EstadoNombre ?? "",
                    MunicipioNombre = x.Comercio.Municipio.MunicipioNombre ?? "",
                    PromedioCalificacion = x.Comercio.CalificacionesComentarios.Any()
                        ? x.Comercio.CalificacionesComentarios.Average(cc => cc.Calificacion)
                        : 0,
                    DistanciaKm = (tipo == "cercanos" || tipo == "sugeridos")
        ? Math.Round(x.Comercio.Ubicacion.Distance(userLocation) * 111.32, 2)
        : null,
                    FechaCreacion = x.Comercio.FechaCreacion,
                    IdUsuario = x.Comercio.IdUsuario
                })
                .ToList();

            var filtrado = lista
                .GroupBy(c => c.IdUsuario)
                .SelectMany(g =>
                {
                    var tieneSuscripcion = g.Any(x => x.SuscripcionVigente);

                    int max = 1;

                    if (tieneSuscripcion)
                    {
                        var tipoPlan = g.First(x => x.SuscripcionVigente).PlanTipo;
                        max = tipoPlan switch
                        {
                            "BUSINESS" => 3,
                            "PRO" => 2,
                            _ => 1
                        };
                    }

                    IEnumerable<ComercioPublicDto> ordenado;

                    if (tieneSuscripcion)
                    {
                        ordenado = g
                            .OrderBy(c => c.FechaCreacion)
                            .Take(1);
                    }
                    else
                    {
                        ordenado = tipo switch
                        {
                            "recientes" => g.OrderByDescending(c => c.FechaCreacion),
                            "cercanos" => g.OrderBy(c => c.DistanciaKm),
                            "sugeridos" => g
                                .OrderByDescending(c => c.PromedioCalificacion)
                                .ThenBy(c => c.DistanciaKm),
                            _ => g.OrderByDescending(c => c.FechaCreacion),
                        };
                    }

                    return ordenado.Take(max);
                })
                .Select(c =>
                {
                    c.IdUsuario = 0; // ocultas usuario
                    return c;
                })
                .ToList();


            var total = filtrado.Count;


            var paginados = filtrado
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (paginados, total);
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

        public async Task<Comercio?> GetComercioByUser(long idUser)
        {
            try
            {
                return await _context.Comercios
                    .AsNoTracking()
                    .Where(c =>
                        c.IdUsuario == idUser &&
                        c.Activo
                    )
                    .OrderBy(c => c.FechaCreacion)
                    .FirstOrDefaultAsync();
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
        public async Task<(List<ComercioPublicDto> items, int total)> GetByFiltros(
            int estadoId,
            int municipioId,
            long idTipoComercio,
            string orden,
            int page,
            int pageSize
        )
        {
            var hoy = DateTime.UtcNow.Date;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 8;
            if (pageSize > 50) pageSize = 50;

            var suscripcionesVigentes = _context.Suscripcions
                .Where(s =>
                    s.IsActive &&
                    !s.IsDeleted &&
                    s.CurrentPeriodEnd > hoy &&
                    (s.Plan.Tipo == "FREE" || s.Plan.Tipo == "BASIC" ||
                     s.Plan.Tipo == "PRO" || s.Plan.Tipo == "BUSINESS")
                )
                .Select(s => new
                {
                    s.UsuarioId,
                    s.CurrentPeriodEnd,
                    s.Plan.BadgeTexto,
                    s.Plan.Tipo
                })
                .AsEnumerable()
                .GroupBy(s => s.UsuarioId)
                .Select(g => g.OrderByDescending(x => x.CurrentPeriodEnd).First())
                .ToList();

            IQueryable<Comercio> comerciosQuery = _context.Comercios
                .Where(c => c.Activo)
                .Include(c => c.Estado)
                .Include(c => c.Municipio)
                .Include(c => c.CalificacionesComentarios);

            var query =
                from c in comerciosQuery.AsEnumerable()
                join s in suscripcionesVigentes
                    on c.IdUsuario equals s.UsuarioId into sus
                from suscripcion in sus.DefaultIfEmpty()
                select new
                {
                    Comercio = c,
                    Suscripcion = suscripcion
                };

            if (estadoId != 0)
                query = query.Where(x => x.Comercio.EstadoId == estadoId);

            if (municipioId != 0)
                query = query.Where(x => x.Comercio.MunicipioId == municipioId);
            if (idTipoComercio != 0)
                query = query.Where(x => x.Comercio.TipoComercioId == idTipoComercio);

            var lista = query
                .Select(x => new ComercioPublicDto
                {
                    Id = x.Comercio.Id,
                    Nombre = x.Comercio.Nombre,
                    Direccion = x.Comercio.Direccion,
                    Telefono = x.Comercio.Telefono,
                    Email = x.Comercio.Email,
                    LogoUrl = x.Comercio.LogoUrl,
                    Lat = x.Comercio.Ubicacion!.Y,
                    Lng = x.Comercio.Ubicacion!.X,
                    ColorPrimario = x.Comercio.ColorPrimario,
                    ColorSecundario = x.Comercio.ColorSecundario,
                    Badge = x.Suscripcion?.BadgeTexto ?? "",
                    PlanTipo = x.Suscripcion?.Tipo,
                    SuscripcionVigente = x.Suscripcion != null,
                    EstadoNombre = x.Comercio.Estado.EstadoNombre ?? "",
                    MunicipioNombre = x.Comercio.Municipio.MunicipioNombre ?? "",
                    PromedioCalificacion = x.Comercio.CalificacionesComentarios.Any()
                        ? x.Comercio.CalificacionesComentarios.Average(cc => cc.Calificacion)
                        : 0,
                    FechaCreacion = x.Comercio.FechaCreacion,
                    IdUsuario = x.Comercio.IdUsuario
                })
                .ToList();
   
            var filtrado = lista
                .GroupBy(c => c.IdUsuario)
                .SelectMany(g =>
                {
                    int max = 1;

                    if (g.Any(x => x.SuscripcionVigente))
                    {
                        var tipoPlan = g.First(x => x.SuscripcionVigente).PlanTipo;
                        max = tipoPlan switch
                        {
                            "BUSINESS" => 3,
                            "PRO" => 2,
                            _ => 1
                        };
                    }

                    return g
                        .OrderByDescending(c => c.FechaCreacion)
                        .Take(max);
                })
                .Select(c =>
                {
                    c.IdUsuario = 0;
                    return c;
                })
                .ToList();
        filtrado = orden.ToLower() switch
            {
                "recientes" => filtrado.OrderByDescending(c => c.FechaCreacion).ToList(),
                "antiguos" => filtrado.OrderBy(c => c.FechaCreacion).ToList(),
                "populares" => filtrado.OrderByDescending(c => c.PromedioCalificacion).ToList(),
                "az" or "alfabetico" => filtrado.OrderBy(c => NormalizarNombreNegocio(c.Nombre)).ToList(),
                _ => filtrado.OrderBy(c => NormalizarNombreNegocio(c.Nombre)).ToList()
            };

    
            var total = filtrado.Count;

            var paginados = filtrado
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (paginados, total);
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

            IQueryable<Usuario> queryColaboradores = _context.Usuarios.Where(c => c.Rol == "Colaborador");

            var badge = await _context.Suscripcions
                .Where(s =>
                    s.UsuarioId == idUser &&
                    s.IsActive &&
                    !s.IsDeleted &&
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
                    idColaborador = queryColaboradores
    .Where(uc => uc.ComercioId == c.Id)
    .Select(uc => uc.Id)
    .FirstOrDefault(),
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
        public async Task<ApiResponse<PagedResponse<UsuarioInfoDto>>> getAllColaboradores(long idComercio, int page = 1,int pageSize = 10)
        {
            IQueryable<Usuario> query = _context.Usuarios.Where(c => c.ComercioId == idComercio && c.Rol == "Colaborador");
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);


            var items = await query
                .OrderByDescending(c => c.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new UsuarioInfoDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Email = c.Email,
                    FotoUrl = c.FotoUrl,
                    Activo = c.Activo,
                    FechaCreacion = c.FechaCreacion,
                    Rol = c.Rol,
                    ComercioId = c.ComercioId,

                })
                .ToListAsync();
            var pagedResponse = new PagedResponse<UsuarioInfoDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                Items = items
            };

            return ApiResponse<PagedResponse<UsuarioInfoDto>>.Success(
                pagedResponse,
                "Listado de comercios obtenido correctamente"
            );
        }
        private static string NormalizarNombreNegocio(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return string.Empty;

            // Quitar artículos iniciales
            nombre = Regex.Replace(
                nombre.Trim(),
                @"^(la|el|los|las)\s+",
                "",
                RegexOptions.IgnoreCase
            );

            // Minúsculas + quitar acentos
            nombre = nombre
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            var chars = nombre
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();

            return new string(chars);
        }

    }
}
