using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using NetTopologySuite.Geometries;

namespace AdLocalAPI.Services
{
    public class ComercioService
    {
        private readonly ComercioRepository _repository;
        private readonly JwtContext _jwtContext;

        public ComercioService(ComercioRepository repository, JwtContext jwtContext)
        {
            _repository = repository;
            _jwtContext = jwtContext;
        }

        // 🔹 Obtener todos los comercios
        public async Task<ApiResponse<object>> GetAllComercios()
        {
            try
            {
                var comercios = await _repository.GetAllAsync();

                return ApiResponse<object>.Success(
                    comercios,
                    "Listado de comercios obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        // 🔹 Obtener comercio por ID
        public async Task<ApiResponse<object>> GetComercioById(int id)
        {
            try
            {
                var comercio = await _repository.GetByIdAsync(id);

                if (comercio == null)
                    return ApiResponse<object>.Error("404", "Comercio no encontrado");

                return ApiResponse<object>.Success(
                    comercio,
                    "Comercio obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
        // 🔹 Obtener comercio por ID
        public async Task<ApiResponse<object>> GetComercioByUser()
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                var comercio = await _repository.GetComercioByUser(idUser);

                if (comercio == null)
                    return ApiResponse<object>.Error("404", "Comercio no encontrado");

                var dto = new ComercioMineDto
                {
                    Id = comercio.Id,
                    Nombre = comercio.Nombre,
                    Direccion = comercio.Direccion,
                    Telefono = comercio.Telefono,
                    Activo = comercio.Activo,
                    LogoBase64 = comercio.LogoUrl,
                    Lat = comercio.Ubicacion != null ? comercio.Ubicacion.Y : null,
                    Lng = comercio.Ubicacion != null ? comercio.Ubicacion.X : null
                };

                return ApiResponse<object>.Success(
                    dto,
                    "Comercio obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }


        public async Task<ApiResponse<object>> CreateComercio(ComercioCreateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return ApiResponse<object>.Error(
                        "400",
                        "El nombre del comercio es obligatorio"
                    );

                if (dto.Lat == 0 || dto.Lng == 0)
                    return ApiResponse<object>.Error(
                        "400",
                        "La ubicación del comercio es obligatoria"
                    );

                long userId = _jwtContext.GetUserId();

                string? logoUrl = null;

                if (!string.IsNullOrWhiteSpace(dto.LogoBase64))
                {
                    string? contentType = TiposImagenPermitidos
                        .FirstOrDefault(x => dto.LogoBase64.StartsWith(x.Value))
                        .Key;

                    if (contentType == null)
                    {
                        return ApiResponse<object>.Error(
                            "400",
                            "Formato de imagen no permitido. Usa JPG, PNG o WEBP"
                        );
                    }

                    string base64Clean = dto.LogoBase64
                        .Replace($"data:{contentType};base64,", string.Empty);

                    byte[] imageBytes = Convert.FromBase64String(base64Clean);

                    logoUrl = await _repository.UploadToSupabaseAsync(
                        imageBytes,
                        (int)userId,
                        contentType
                    );
                }

                var comercio = new Comercio
                {
                    Nombre = dto.Nombre,
                    Direccion = dto.Direccion,
                    Telefono = dto.Telefono,
                    LogoUrl = logoUrl,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    IdUsuario = userId,
                    Ubicacion = new NetTopologySuite.Geometries.Point(dto.Lng, dto.Lat)
                    {
                        SRID = 4326
                    }
                };

                var creado = await _repository.CreateAsync(comercio);

                var responseDto = new ComercioMineDto
                {
                    Id = comercio.Id,
                    Nombre = comercio.Nombre,
                    Direccion = comercio.Direccion,
                    Telefono = comercio.Telefono,
                    Activo = comercio.Activo,
                    LogoBase64 = comercio.LogoUrl,
                    Lat = comercio.Ubicacion?.Y,
                    Lng = comercio.Ubicacion?.X
                };

                return ApiResponse<object>.Success(
                    responseDto,
                    "El comercio se creó correctamente"
                );
            }
            catch (FormatException)
            {
                return ApiResponse<object>.Error(
                    "400",
                    "La imagen enviada no tiene un formato Base64 válido"
                );
            }
            catch
            {
                return ApiResponse<object>.Error(
                    "500",
                    "Ocurrió un error al crear el comercio"
                );
            }
        }



        // 🔹 Actualizar comercio
        public async Task<ApiResponse<object>> UpdateComercio(int id, ComercioUpdateDto dto)
        {
            try
            {
                var comercio = await _repository.GetByIdAsync(id);

                if (comercio == null)
                    return ApiResponse<object>.Error(
                        "404",
                        "Comercio no encontrado"
                    );

                long userId = _jwtContext.GetUserId();

                if (comercio.IdUsuario != userId)
                    return ApiResponse<object>.Error(
                        "403",
                        "No tienes permiso para modificar este comercio"
                    );

                comercio.Nombre = dto.Nombre;
                comercio.Direccion = dto.Direccion;
                comercio.Telefono = dto.Telefono;
                comercio.Activo = dto.Activo;

                if (!string.IsNullOrWhiteSpace(dto.LogoBase64) &&
                    !EsUrl(dto.LogoBase64))
                {
                    if (!EsImagenBase64(dto.LogoBase64))
                    {
                        return ApiResponse<object>.Error(
                            "400",
                            "Formato de imagen inválido"
                        );
                    }

                    string? contentType = TiposImagenPermitidos
                        .FirstOrDefault(x => dto.LogoBase64.StartsWith(x.Value))
                        .Key;

                    if (contentType == null)
                    {
                        return ApiResponse<object>.Error(
                            "400",
                            "Formato de imagen no permitido. Usa JPG, PNG o WEBP"
                        );
                    }

                    string base64Clean = dto.LogoBase64.Replace(
                        $"data:{contentType};base64,", string.Empty
                    );

                    byte[] imageBytes = Convert.FromBase64String(base64Clean);

                    if (!string.IsNullOrWhiteSpace(comercio.LogoUrl))
                    {
                        await _repository.DeleteFromSupabaseByUrlAsync(comercio.LogoUrl);
                    }

                    comercio.LogoUrl = await _repository.UploadToSupabaseAsync(
                        imageBytes,
                        (int)userId,
                        contentType
                    );
                }


                if (
                    dto.Lat.HasValue &&
                    dto.Lng.HasValue &&
                    !double.IsNaN(dto.Lat.Value) &&
                    !double.IsNaN(dto.Lng.Value) &&
                    !double.IsInfinity(dto.Lat.Value) &&
                    !double.IsInfinity(dto.Lng.Value)
                )
                {
                    comercio.Ubicacion = new NetTopologySuite.Geometries.Point(
                        dto.Lng.Value,
                        dto.Lat.Value
                    )
                    {
                        SRID = 4326
                    };
                }
                else
                {
                    comercio.Ubicacion = null;
                }


                await _repository.UpdateAsync(comercio);

                var responseDto = new ComercioMineDto
                {
                    Id = comercio.Id,
                    Nombre = comercio.Nombre,
                    Direccion = comercio.Direccion,
                    Telefono = comercio.Telefono,
                    Activo = comercio.Activo,
                    LogoBase64 = comercio.LogoUrl,
                    Lat = comercio.Ubicacion?.Y,
                    Lng = comercio.Ubicacion?.X
                };


                return ApiResponse<object>.Success(
                    responseDto,
                    "El comercio se actualizó correctamente"
                );
            }
            catch (FormatException)
            {
                return ApiResponse<object>.Error(
                    "400",
                    "La imagen enviada no tiene un formato Base64 válido"
                );
            }
            catch
            {
                return ApiResponse<object>.Error(
                    "500",
                    "Ocurrió un error al actualizar el comercio"
                );
            }
        }


        // 🔹 Eliminar comercio
        public async Task<ApiResponse<object>> DeleteComercio(int id)
        {
            try
            {
                var comercio = await _repository.GetByIdAsync(id);

                if (comercio == null)
                    return ApiResponse<object>.Error(
                        "404",
                        "Comercio no encontrado"
                    );

                long userId = _jwtContext.GetUserId();
                if (comercio.IdUsuario != userId)
                {
                    return ApiResponse<object>.Error(
                        "403",
                        "No tienes permiso para eliminar este comercio"
                    );
                }

                if (!string.IsNullOrWhiteSpace(comercio.LogoUrl))
                {
                    await _repository.DeleteFromSupabaseByUrlAsync(comercio.LogoUrl);
                }
                await _repository.DeleteAsync(id);

                return ApiResponse<object>.Success(
                    null,
                    "El comercio se eliminó correctamente"
                );
            }
            catch
            {
                return ApiResponse<object>.Error(
                    "500",
                    "Ocurrió un error al eliminar el comercio"
                );
            }
        }

        private static readonly Dictionary<string, string> TiposImagenPermitidos = new()
        {
            { "image/jpeg", "data:image/jpeg;base64," },
            { "image/jpg",  "data:image/jpg;base64,"  },
            { "image/png",  "data:image/png;base64,"  },
            { "image/webp", "data:image/webp;base64," }
        };
        private bool EsUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out _);
        }
        private bool EsImagenBase64(string value)
        {
            return value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
        }



    }
}
