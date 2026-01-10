using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using NetTopologySuite.Geometries;

namespace AdLocalAPI.Services
{
    public class ComercioService
    {
        private readonly ComercioRepository _repository;
        private readonly JwtContext _jwtContext;
        private readonly IRelComercioImagenRepositorio _comercioImagenRepositorio;

        public ComercioService(ComercioRepository repository, JwtContext jwtContext, IRelComercioImagenRepositorio comercioImagenRepositorio)
        {
            _repository = repository;
            _jwtContext = jwtContext;
            _comercioImagenRepositorio = comercioImagenRepositorio;
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
        public async Task<ApiResponse<ComercioMineDto>> GetComercioById(int id)
        {
            try
            {
                var comercio = await _repository.GetByIdAsync(id);

                if (comercio == null)
                    return ApiResponse<ComercioMineDto>.Error("404", "Comercio no encontrado");

                var listaImagenes = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                List<string> Imagenes = new List<string>();
                if (listaImagenes.Count > 0)
                {
                    foreach (var item in listaImagenes)
                    {
                        Imagenes.Add(item.FotoUrl);
                    }
                }

                var dto = new ComercioMineDto
                {
                    Id = comercio.Id,
                    Nombre = comercio.Nombre,
                    Direccion = comercio.Direccion,
                    Telefono = comercio.Telefono,
                    Descripcion = comercio.Descripcion,
                    Email = comercio.Email,
                    Activo = comercio.Activo,
                    LogoBase64 = comercio.LogoUrl,
                    Lat = comercio.Ubicacion.Y,
                    Lng = comercio.Ubicacion.X,
                    ColorPrimario = comercio.ColorPrimario,
                    ColorSecundario = comercio.ColorSecundario,
                    Imagenes = Imagenes,
                };

                return ApiResponse<ComercioMineDto>.Success(
                    dto,
                    "Comercio obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<ComercioMineDto>.Error("500", ex.Message);
            }
        }
        // 🔹 Obtener comercio por ID
        public async Task<ApiResponse<ComercioMineDto>> GetComercioByUser()
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                var comercio = await _repository.GetComercioByUser(idUser);

                if (comercio == null)
                    return ApiResponse<ComercioMineDto>.Error("404", "Comercio no encontrado");

                var listaImagenes = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                List<string> Imagenes = new List<string>();
                if (listaImagenes.Count > 0)
                {
                    foreach (var item in listaImagenes)
                    {
                        Imagenes.Add(item.FotoUrl);
                    }
                }

                var dto = new ComercioMineDto
                {
                    Id = comercio.Id,
                    Nombre = comercio.Nombre,
                    Direccion = comercio.Direccion,
                    Telefono = comercio.Telefono,
                    Descripcion = comercio.Descripcion,
                    Email = comercio.Email,
                    Activo = comercio.Activo,
                    LogoBase64 = comercio.LogoUrl,
                    Lat = comercio.Ubicacion.Y,
                    Lng = comercio.Ubicacion.X,
                    ColorPrimario = comercio.ColorPrimario,
                    ColorSecundario = comercio.ColorSecundario,
                    Imagenes = Imagenes,
                };

                return ApiResponse<ComercioMineDto>.Success(
                    dto,
                    "Comercio obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<ComercioMineDto>.Error("500", ex.Message);
            }
        }


        public async Task<ApiResponse<object>> CreateComercio(ComercioCreateDto dto)
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                var comercioExistente = await _repository.GetComercioByUser(idUser);

                if (comercioExistente != null)
                {
                    return ApiResponse<object>.Error(
                        "409",
                        "Ya existe un comercio registrado asociado a este usuario"
                    );
                }
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
                    ColorPrimario = dto.ColorPrimario,
                    ColorSecundario = dto.ColorSecundario,
                    Descripcion = dto.Descripcion,      
                    Email = dto.Email,
                    Ubicacion = new NetTopologySuite.Geometries.Point(dto.Lng, dto.Lat)
                    {
                        SRID = 4326
                    }
                };

                var creado = await _repository.CreateAsync(comercio);

                if (dto.Imagenes?.Count > 0)
                {
                    foreach (var item in dto.Imagenes)
                    {
                        try
                        {
                            string? contentType = TiposImagenPermitidos
                                .FirstOrDefault(x => item.StartsWith(x.Value))
                                .Key;

                            if (contentType == null)
                                continue;

                            string base64Clean = item
                                .Replace($"data:{contentType};base64,", string.Empty);

                            byte[] imageBytes = Convert.FromBase64String(base64Clean);

                            var imagenUrl = await _comercioImagenRepositorio
                                .UploadToSupabaseAsync(imageBytes, (int)userId, contentType);

                            if (string.IsNullOrEmpty(imagenUrl))
                                continue;

                            await _comercioImagenRepositorio.Crear(creado.Id, imagenUrl);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                var responseDto = new ComercioMineDto
                {
                    Id = creado.Id,
                    Nombre = creado.Nombre,
                    Direccion = creado.Direccion,
                    Telefono = creado.Telefono,
                    Descripcion = creado.Descripcion,
                    Email = creado.Email,
                    Activo = creado.Activo,
                    LogoBase64 = creado.LogoUrl,
                    Lat = creado.Ubicacion.Y,
                    Lng = creado.Ubicacion.X,
                    ColorPrimario = creado.ColorPrimario,
                    ColorSecundario = creado.ColorSecundario,
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
        public async Task<ApiResponse<object>> UpdateComercio(ComercioUpdateDto dto)
        {
            try
            {
                int idComercio = (int)_jwtContext.GetComercioId();
                var comercio = await _repository.GetByIdAsync(idComercio);

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
                comercio.Descripcion = dto.Descripcion;
                comercio.ColorPrimario = dto.ColorPrimario;
                comercio.ColorSecundario = dto.ColorSecundario;
                comercio.Email = dto.Email;
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
                    !double.IsNaN(dto.Lat) &&
                    !double.IsNaN(dto.Lng) &&
                    !double.IsInfinity(dto.Lat) &&
                    !double.IsInfinity(dto.Lng)
                )
                {
                    comercio.Ubicacion = new NetTopologySuite.Geometries.Point(
                        dto.Lng,
                        dto.Lat
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

                if (dto.Imagenes?.Count > 0)
                {
                    var imagenesActuales = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                    var urlsRecibidas = dto.Imagenes.Where(x => !string.IsNullOrWhiteSpace(x) && EsUrl(x)).ToList();
                    foreach (var img in imagenesActuales)
                    {
                        if (!urlsRecibidas.Contains(img.FotoUrl))
                        {
                            await _comercioImagenRepositorio
                                .Eliminar(comercio.Id, img.FotoUrl);
                        }
                    }

                    foreach (var item in dto.Imagenes)
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(item) &&!EsUrl(item)) 
                            {
                                string? contentType = TiposImagenPermitidos.FirstOrDefault(x => item.StartsWith(x.Value)).Key;

                                if (contentType == null)
                                    continue;

                                string base64Clean = item
                                    .Replace($"data:{contentType};base64,", string.Empty);

                                byte[] imageBytes = Convert.FromBase64String(base64Clean);

                                var imagenUrl = await _comercioImagenRepositorio
                                    .UploadToSupabaseAsync(imageBytes, (int)userId, contentType);

                                if (string.IsNullOrEmpty(imagenUrl))
                                    continue;

                                await _comercioImagenRepositorio
                                    .Crear(comercio.Id, imagenUrl);
                            }
  
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                var responseDto = new ComercioMineDto
                {
                    Id = comercio.Id,
                    Nombre = comercio.Nombre,
                    Direccion = comercio.Direccion,
                    Telefono = comercio.Telefono,
                    Descripcion = comercio.Descripcion,
                    Email = comercio.Email,
                    Activo = comercio.Activo,
                    LogoBase64 = comercio.LogoUrl,
                    Lat = comercio.Ubicacion.Y,
                    Lng = comercio.Ubicacion.X,
                    ColorPrimario = comercio.ColorPrimario,
                    ColorSecundario = comercio.ColorSecundario,
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
                var listaImagenes = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                if (listaImagenes.Count > 0)
                {
                    foreach (var img in listaImagenes)
                    {
                        await _comercioImagenRepositorio
                            .Eliminar(comercio.Id, img.FotoUrl);
                    }
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
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps;
        }
        private bool EsImagenBase64(string value)
        {
            return value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
