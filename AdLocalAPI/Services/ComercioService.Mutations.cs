using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services
{
    public partial class ComercioService
    {
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

        public async Task<ApiResponse<object>> CreateComercio(ComercioCreateDto dto)
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                string planTipo = _jwtContext.GetPlanTipo();
                if (planTipo != "PRO" && planTipo != "BUSINESS")
                {
                    var comercioExistente = await _repository.GetComercioByUser(idUser);

                    if (comercioExistente != null)
                    {
                        return ApiResponse<object>.Error(
                            "409",
                            "Ya existe un comercio registrado asociado a este usuario"
                        );
                    }
                }

                if (planTipo == "PRO" || planTipo == "BUSINESS") 
                {
                    int maxNegocios = _jwtContext.GetMaxNegocios();
                    List<Comercio> totalNegocios = await _repository.GetAllComerciosByIdUsuario(idUser);
                    if (maxNegocios == totalNegocios.Count)
                    {
                        return ApiResponse<object>.Error(
                            "409",
                            "Has alcanzado el límite de comercios permitidos por tu plan."
                        );
                    }
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
                if (dto.EstadoId == 0)                
                    return ApiResponse<object>.Error(
                       "400",
                       "El estado del comercio es obligatorrio"
                   );
                if (dto.MunicipioId == 0)                
                    return ApiResponse<object>.Error(
                       "400",
                       "El municipio del comercio es obligatorrio"
                   );               
                if (dto.TipoComercioId == 0)                
                    return ApiResponse<object>.Error(
                       "400",
                       "El tipo del comercio es obligatorrio"
                   );               

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

                    logoUrl = await _repository.UploadImageAsync(
                        imageBytes,
                        (int)idUser,
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
                    Visible = true,
                    Uuid = Guid.NewGuid(),
                    FechaCreacion = DateTime.UtcNow,
                    IdUsuario = idUser,
                    ColorPrimario = dto.ColorPrimario,
                    ColorSecundario = dto.ColorSecundario,
                    Descripcion = dto.Descripcion,
                    Email = dto.Email,
                    EstadoId = dto.EstadoId,
                    MunicipioId = dto.MunicipioId,
                    TipoComercioId = dto.TipoComercioId,
                    Ubicacion = new NetTopologySuite.Geometries.Point(dto.Lng, dto.Lat)
                    {
                        SRID = 4326
                    }
                };

                var creado = await _repository.CreateAsync(comercio);

                if (dto.Horarios.Count > 0)
                {
                    await _horarioComercioRepository.CrearHorariosAsync(
                        creado.Id,
                        dto.Horarios.ToList()
                    );
                }

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
                                .UploadImageAsync(imageBytes, (int)idUser, contentType);

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

                return ApiResponse<object>.Success(
                    null,
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

        public async Task<ApiResponse<object>> UpdateComercio(ComercioUpdateDto dto)
        {
            try
            {
                long idComercio = dto.Id == 0 ? _jwtContext.GetComercioId() : dto.Id;
                var comercio = await _repository.GetByIdAsync(idComercio);

                if (comercio == null)
                    return ApiResponse<object>.Error(
                        "404",
                        "Comercio no encontrado"
                    );
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
                if (dto.EstadoId == 0)
                    return ApiResponse<object>.Error(
                       "400",
                       "El estado del comercio es obligatorrio"
                   );
                if (dto.MunicipioId == 0)
                    return ApiResponse<object>.Error(
                       "400",
                       "El municipio del comercio es obligatorrio"
                   );
                if (dto.TipoComercioId == 0)
                    return ApiResponse<object>.Error(
                       "400",
                       "El tipo del comercio es obligatorrio"
                   );
                long userId = 0;
                if (_jwtContext.GetUserRole() == "Colaborador")
                {
                    var usuario = await _usuarioRepository.GetByIdComercioAsync(_jwtContext.GetComercioId());                    
                    if (usuario == null)
                    {
                        return ApiResponse<object>.Error(
                           "404",
                           "No se encontró el comercio asociado al colaborador."
                       );
                    }
                    userId = usuario.Id;
                    var planActivo = await _suscripcionRepository.GetActivaByUsuarioAsync(userId);
                    
                    if (planActivo?.Plan == null || planActivo.Plan.Tipo == "BASIC" || planActivo.Plan.Tipo == "FREE")
                    {
                        return ApiResponse<object>.Error(
                           "400",
                           "El dueño del negocio necesita actualizar su suscripción para que puedas usar las funciones de colaborador."
                       );
                    }
                }
                else 
                {
                    userId = _jwtContext.GetUserId();
                }

                if (comercio.IdUsuario != userId)
                    return ApiResponse<object>.Error(
                        "403",
                        "No tienes permiso para modificar este comercio"
                    );
                comercio.EstadoId = dto.EstadoId;
                comercio.MunicipioId = dto.MunicipioId;
                comercio.Nombre = dto.Nombre;
                comercio.Direccion = dto.Direccion;
                comercio.Telefono = dto.Telefono;
                comercio.Descripcion = dto.Descripcion;
                comercio.ColorPrimario = dto.ColorPrimario;
                comercio.ColorSecundario = dto.ColorSecundario;
                comercio.Email = dto.Email;
                comercio.Activo = dto.Activo;
                comercio.TipoComercioId = dto.TipoComercioId;

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
                        await _repository.DeleteFromS3Async(comercio.LogoUrl);
                    }

                    comercio.LogoUrl = await _repository.UploadImageAsync(
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

                if (dto.Horarios.Count > 0)
                {
                    bool siTiene = await _horarioComercioRepository.ComercioTieneHorariosAsync(comercio.Id);
                    if (siTiene)
                    {
                        await _horarioComercioRepository.ActualizarHorariosAsync(comercio.Id,dto.Horarios.ToList());
                    }
                    else 
                    {                      
                        await _horarioComercioRepository.CrearHorariosAsync(comercio.Id,dto.Horarios.ToList());
                    }
                }

                if (dto.Imagenes?.Count > 0)
                {
                    var imagenesActuales = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                    var urlsRecibidas = dto.Imagenes.Where(x => !string.IsNullOrWhiteSpace(x) && EsUrl(x)).ToList();
                    foreach (var img in imagenesActuales)
                    {
                        if (!urlsRecibidas.Contains(img.FotoUrl))
                        {
                            await _comercioImagenRepositorio.Eliminar(comercio.Id, img.FotoUrl);
                        }
                    }

                    foreach (var item in dto.Imagenes)
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(item))
                                continue;

                            if (EsImagenBase64(item))  
                            {
                                string? contentType = TiposImagenPermitidos
                                    .FirstOrDefault(x => item.StartsWith(x.Value))
                                    .Key;
                                if (contentType == null)
                                    continue;

                                string base64Clean = item.Replace($"data:{contentType};base64,", string.Empty);
                                byte[] imageBytes = Convert.FromBase64String(base64Clean);

                                var storageKey = await _comercioImagenRepositorio.UploadImageAsync(
                                    imageBytes,
                                    (long)comercio.Id,  
                                    contentType
                                );

                                if (string.IsNullOrEmpty(storageKey))
                                    continue;

                                await _comercioImagenRepositorio.Crear(comercio.Id, storageKey);
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }

                return ApiResponse<object>.Success(
                    null,
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

        public async Task<ApiResponse<object>> DeleteComercio(long id)
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
                    await _repository.DeleteFromS3Async(comercio.LogoUrl);
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
                bool siTiene = await _horarioComercioRepository.ComercioTieneHorariosAsync(comercio.Id);
                if (siTiene)
                {
                    await _horarioComercioRepository.EliminarHorariosPorComercioAsync(comercio.Id);
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
    }
}
