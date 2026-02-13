using AdLocalAPI.Data;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Models;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace AdLocalAPI.Repositories
{
    public class RelComercioImagenRepositorio : IRelComercioImagenRepositorio
    {
        private readonly AppDbContext _context;
        private readonly IAmazonS3 _s3Client;             
        private readonly IWebHostEnvironment _env;
        private readonly string _bucketName = "relcomercioimagen";           

        public RelComercioImagenRepositorio(
            AppDbContext context,
            IAmazonS3 s3Client,
            IWebHostEnvironment env
            )                  
        {
            _context = context;
            _s3Client = s3Client;
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

            if (imagen == null) return false;

            // Opcional: eliminar la foto anterior en S3 si ya no se usa
            await DeleteFromS3Async(fotoUrlActual);   // descomenta si quieres borrar la vieja

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

            if (imagen == null) return false;

            bool deletedFromS3 = await DeleteFromS3Async(fotoUrl);

            if (!deletedFromS3)
            {

                Console.WriteLine($"No se pudo eliminar de S3: {fotoUrl}");
            }

            _context.RelComercioImagen.Remove(imagen);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> UploadImageAsync(byte[] imageBytes, long comercioId, string contentType = "image/png")
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

                string fileName = $"{envPrefix}_RelComercioImagen{comercioId}_{DateTime.UtcNow.Ticks}{extension}";

                string key = $"rel-comercio-imagen/{fileName}";

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


                return "https://pub-e3c19952fb9f47fc8e427d2a7889c66f.r2.dev/" + key;
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString);
                return null;
            }


        }


        private async Task<bool> DeleteFromS3Async(string storageReference)
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
    }
}