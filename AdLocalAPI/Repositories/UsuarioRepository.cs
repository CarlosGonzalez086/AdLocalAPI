using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class UsuarioRepository
    {
        private readonly AppDbContext _context;
        private readonly Supabase.Client _supabaseClient;
        private readonly IWebHostEnvironment _env;
        private readonly string _bucketName = "perfil";
        private readonly IAmazonS3 _s3Client;

        public UsuarioRepository(AppDbContext context, Supabase.Client supabaseClient, IWebHostEnvironment env, IAmazonS3 s3Client)
        {
            _context = context;
            _supabaseClient = supabaseClient;
            _s3Client = s3Client;
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

        public async Task<object> GetAllAsync(
            int page,
            int pageSize,
            string orderBy,
            string search)
        {
            var now = DateTime.UtcNow;

            // 1️ Query base (SOLO filtros)
            IQueryable<Usuario> baseQuery = _context.Usuarios
                                            .AsNoTracking()
                                            .Where(u => u.Rol == "Comercio")
                                            .Where(u =>
                                                u.Suscripciones.Any(s =>
                                                    s.IsActive &&
                                                    !s.IsDeleted &&
                                                    (s.Status == "active" || s.Status == "canceling") &&
                                                    s.CurrentPeriodEnd >= now
                                                )
                                            );

            // 2️ Búsqueda
            if (!string.IsNullOrWhiteSpace(search))
            {
                baseQuery = baseQuery.Where(u =>
                    EF.Functions.ILike(u.Nombre, $"%{search}%") ||
                    EF.Functions.ILike(u.Email, $"%{search}%")
                );
            }

            // 3️⃣ TOTAL (NO se traba)
            var totalRecords = await baseQuery.CountAsync();

            // 4️⃣ Proyección (usuario + su suscripción activa)
            var query = baseQuery.Select(u => new
            {
                Usuario = new
                {
                    u.Id,
                    u.Nombre,
                    u.Email,
                    u.FechaCreacion,
                    u.FotoUrl,
                },

                Suscripcion = u.Suscripciones
                    .Where(s =>
                        s.IsActive &&
                        !s.IsDeleted &&
                        (s.Status == "active" || s.Status == "canceling") &&
                        s.CurrentPeriodEnd >= now
                    )
                    .Select(s => new
                    {
                        s.Status,
                        s.CurrentPeriodStart,
                        s.CurrentPeriodEnd,
                        s.AutoRenew,
                        Plan = new
                        {
                            s.Plan.Id,
                            s.Plan.Nombre,
                            s.Plan.Tipo,
                            s.Plan.Precio,
                            s.Plan.MaxFotos
                        }
                    })
                    .FirstOrDefault()
            });


            // 5️⃣ Orden
            query = orderBy switch
            {
                "recent" => query.OrderByDescending(x => x.Usuario.FechaCreacion),
                "old" => query.OrderBy(x => x.Usuario.FechaCreacion),
                "az" => query.OrderBy(x => x.Usuario.Nombre),
                "za" => query.OrderByDescending(x => x.Usuario.Nombre),
                _ => query.OrderByDescending(x => x.Usuario.FechaCreacion)
            };

            // 6️⃣ Paginación
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new
            {
                totalRecords,
                page,
                pageSize,
                data
            };
        }




        public async Task<Usuario?> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Usuarios
                                     .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                // Aquí puedes loguear el error si quieres
                Console.WriteLine($"Error al obtener usuario por Id {id}: {ex.Message}");
                // Opcionalmente podrías lanzar otra excepción o devolver null
                return null;
            }
        }

        public async Task<Usuario> GetByIdComercioAsync(long id)
        {
            Usuario usuario = new Usuario();
            try
            {

                usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Comercios.Any(c => c.Id == id));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener usuario por Id {id}: {ex.Message}");
                return null;
            }
            return usuario;
        }
        public async Task<Usuario> GetByIdComercioAndIdUserAsync(long idUser,long idComercio)
        {
            Usuario usuario = new Usuario();
            try
            {
                usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(x =>
                        x.Id == idUser &&
                        x.Rol == "Colaborador" &&
                        x.ComercioId == idComercio
                    );


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener usuario por Id {idUser}: {ex.Message}");
                return null;
            }
            return usuario;
        }

        public async Task<Usuario?> GetByCodeAsync(string code)
        {
            try
            {
                return await _context.Usuarios
                                     .FirstOrDefaultAsync(u => u.Codigo == code);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener usuario por code {code}: {ex.Message}");
                return null;
            }
        }
        public async Task<Usuario?> GetByTokenAsync(string token)
        {
            try
            {
                return await _context.Usuarios
                                     
                                     .FirstOrDefaultAsync(u => u.Token == token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener usuario por token {token}: {ex.Message}");
                return null;
            }
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
        public async Task<string> UploadImageAsync(byte[] imageBytes, long userId, string contentType = "image/png")
        {
            try
            {
                string envPrefix = _env.IsProduction() ? "prod" : "local";
                string extension = contentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    _ => ".png"
                };

                string fileName = $"{envPrefix}_Perfil{userId}_{DateTime.UtcNow.Ticks}{extension}";

                string key = $"perfil-imagen/{fileName}";

                using var stream = new MemoryStream(imageBytes);

                var request = new Amazon.S3.Model.PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = contentType,
                    DisablePayloadSigning = true

                };

                await _s3Client.PutObjectAsync(request);


                return "https://pub-037f80ecc77344f2ae668578f6c3d6e2.r2.dev/" + key;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString);
                return null;
            }
        }
        public async Task<bool> DeleteFromS3Async(string storageReference)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(storageReference)) return false;

                string key;

                if (storageReference.StartsWith("http"))
                {
                    var uri = new Uri(storageReference);
                    var path = uri.AbsolutePath.TrimStart('/');
                    key = path;
                }
                else
                {
                    key = storageReference;
                }

                var request = new Amazon.S3.Model.DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error eliminando de S3: {ex.Message}");
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
        public async Task DeleteColaboradorAsync(long idUser,long idComercio)
        {
            var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(x =>
                        x.Id == idUser &&
                        x.Rol == "Colaborador" &&
                        x.ComercioId == idComercio
                    );
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Usuario?> GetByCodigoReferidoAsync(string codigo)
        {
            try
            {
                return await _context.Usuarios

                                     .FirstOrDefaultAsync(u => u.CodigoReferido == codigo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener usuario por token {codigo}: {ex.Message}");
                return null;
            }
        }
        public async Task<Usuario?> GetByStripeId(string CustomerId)
        {
            try
            {
                return await _context.Usuarios.FirstOrDefaultAsync(u => u.StripeCustomerId == CustomerId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener usuario por token {CustomerId}: {ex.Message}");
                return null;
            }
        }

    }
}
