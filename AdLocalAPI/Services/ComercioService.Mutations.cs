using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Services
{
    public partial class ComercioService
    {
        private bool EsUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps;
        }

        public async Task<ApiResponse<object>> CreateComercio(ComercioCreateDto dto)
        {
            try
            {
                if (!_jwtContext.EstaAutenticado())
                {
                    return ApiResponse<object>.Error("401", "No autenticado.");
                }

                if (!_jwtContext.EsComercio())
                {
                    return ApiResponse<object>.Error("403", "Solo las cuentas de tipo Comercio pueden registrar un negocio.");
                }

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
                    if (maxNegocios <= totalNegocios.Count)
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
                    if (!MediaBase64Validator.TryDecodeValidatedImage(
                            dto.LogoBase64,
                            out byte[] imageBytes,
                            out string contentType,
                            out string validationError))
                    {
                        return ApiResponse<object>.Error("400", validationError);
                    }

                    logoUrl = await _repository.UploadImageAsync(
                        imageBytes,
                        (int)idUser,
                        contentType
                    );
                }

                var comercio = new Comercio
                {
                    ZonaHoraria = CitaOperacion.Zona(dto.ZonaHoraria).Id,
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
                    int maxFotos = _jwtContext.GetMaxFotos();
                    var imagenesPermitidas = maxFotos > 0 ? dto.Imagenes.Take(maxFotos) : dto.Imagenes;
                    foreach (var item in imagenesPermitidas)
                    {
                        if (!MediaBase64Validator.TryDecodeValidatedImage(
                                item,
                                out byte[] imageBytes,
                                out string contentType,
                                out _))
                            continue;

                        try
                        {
                            var imagenUrl = await _comercioImagenRepositorio
                                .UploadImageAsync(imageBytes, (int)idUser, contentType);

                            if (!string.IsNullOrEmpty(imagenUrl))
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
            catch (CitaOperacionException e) { return ApiResponse<object>.Error(e.Codigo, e.Message); }
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
                    var comercioAsignadoId = _jwtContext.GetComercioId();
                    if (comercio.Id != comercioAsignadoId)
                    {
                        return ApiResponse<object>.Error(
                            "403",
                            "No tienes permiso para modificar este comercio"
                        );
                    }

                    var usuario = await _usuarioRepository.GetByIdComercioAsync(comercioAsignadoId);                    
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
                if (!_jwtContext.EsColaborador())
                    comercio.Activo = dto.Activo;
                comercio.TipoComercioId = dto.TipoComercioId;

                string? previousLogoToDelete = null;
                if (!string.IsNullOrWhiteSpace(dto.LogoBase64) &&
                    !EsUrl(dto.LogoBase64))
                {
                    if (!MediaBase64Validator.TryDecodeValidatedImage(
                            dto.LogoBase64,
                            out byte[] imageBytes,
                            out string contentType,
                            out string validationError))
                    {
                        return ApiResponse<object>.Error("400", validationError);
                    }

                    previousLogoToDelete = comercio.LogoUrl;
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

                if (!string.IsNullOrWhiteSpace(previousLogoToDelete))
                {
                    await _repository.DeleteFromS3Async(previousLogoToDelete);
                }

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

                            if (MediaBase64Validator.TryDecodeValidatedImage(
                                    item,
                                    out byte[] imageBytes,
                                    out string contentType,
                                    out _))
                            {
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
            catch (CitaOperacionException e) { return ApiResponse<object>.Error(e.Codigo, e.Message); }
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
    }
}
