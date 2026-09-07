using System;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.Direcciones;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services
{
    public partial class DireccionUsuarioService
    {
        // ============================================================
        // CREAR
        // ============================================================

        public async Task<ApiResponse<DireccionUsuarioResponseDto>> Crear(DireccionUsuarioDto dto)
        {
            try
            {
                if (!_jwtContext.EsCliente())
                {
                    return ApiResponse<DireccionUsuarioResponseDto>.Error(
                        "403",
                        "No tienes autorización para registrar direcciones."
                    );
                }

                if (dto == null)
                {
                    return ApiResponse<DireccionUsuarioResponseDto>.Error(
                        "400",
                        "La información de la dirección es requerida."
                    );
                }

                var validacion = ValidarDireccion(
                    dto.Alias,
                    dto.Calle,
                    dto.NumeroExterior,
                    dto.Colonia,
                    dto.CodigoPostal,
                    dto.IdEstado,
                    dto.IdMunicipio,
                    dto.Latitud,
                    dto.Longitud
                );

                if (validacion != null)
                {
                    return ApiResponse<DireccionUsuarioResponseDto>.Error("400", validacion);
                }

                var existeEstado = await _repository.ExisteEstadoAsync(dto.IdEstado);
                if (!existeEstado)
                {
                    return ApiResponse<DireccionUsuarioResponseDto>.Error("400", "El estado seleccionado no es válido.");
                }

                var existeMunicipio = await _repository.ExisteMunicipioAsync(dto.IdMunicipio);
                if (!existeMunicipio)
                {
                    return ApiResponse<DireccionUsuarioResponseDto>.Error("400", "El municipio seleccionado no es válido.");
                }

                var idUsuario = _jwtContext.GetUserId();

                var tieneDirecciones = await _repository.TieneDireccionesActivasAsync(idUsuario);
                var esPredeterminada = !tieneDirecciones || dto.EsPredeterminada;

                if (esPredeterminada)
                {
                    await _repository.QuitarPredeterminadasAsync(idUsuario);
                }

                var direccion = new DireccionUsuario
                {
                    Uuid = Guid.NewGuid(),
                    IdUsuario = idUsuario,
                    Alias = dto.Alias.Trim(),
                    Calle = dto.Calle.Trim(),
                    NumeroExterior = dto.NumeroExterior.Trim(),
                    NumeroInterior = string.IsNullOrWhiteSpace(dto.NumeroInterior) ? null : dto.NumeroInterior.Trim(),
                    Colonia = dto.Colonia.Trim(),
                    CodigoPostal = dto.CodigoPostal.Trim(),
                    IdEstado = dto.IdEstado,
                    IdMunicipio = dto.IdMunicipio,
                    Latitud = dto.Latitud,
                    Longitud = dto.Longitud,
                    Referencias = string.IsNullOrWhiteSpace(dto.Referencias) ? null : dto.Referencias.Trim(),
                    Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
                    EsPredeterminada = esPredeterminada,
                    Activo = true,
                    Eliminado = false,
                    FechaCreacion = DateTime.UtcNow
                };

                await _repository.CrearAsync(direccion);

                var creada = await _repository.ObtenerPorUuidAsync(idUsuario, direccion.Uuid);

                return ApiResponse<DireccionUsuarioResponseDto>.Success(
                    MapearResponse(creada ?? direccion),
                    "Dirección registrada correctamente."
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<DireccionUsuarioResponseDto>.Error("401", ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse<DireccionUsuarioResponseDto>.Error(
                    "500",
                    $"Ocurrió un error al registrar la dirección: {ex.Message}"
                );
            }
        }

        // ============================================================
        // ACTUALIZAR
        // ============================================================

        public async Task<ApiResponse<bool>> Actualizar(Guid uuid, DireccionUsuarioDto dto)
        {
            try
            {
                if (!_jwtContext.EsCliente())
                {
                    return ApiResponse<bool>.Error("403", "No tienes autorización para actualizar direcciones.");
                }

                if (uuid == Guid.Empty)
                {
                    return ApiResponse<bool>.Error("400", "La dirección es requerida.");
                }

                if (dto == null)
                {
                    return ApiResponse<bool>.Error("400", "La información de la dirección es requerida.");
                }

                var validacion = ValidarDireccion(
                    dto.Alias,
                    dto.Calle,
                    dto.NumeroExterior,
                    dto.Colonia,
                    dto.CodigoPostal,
                    dto.IdEstado,
                    dto.IdMunicipio,
                    dto.Latitud,
                    dto.Longitud
                );

                if (validacion != null)
                {
                    return ApiResponse<bool>.Error("400", validacion);
                }

                var idUsuario = _jwtContext.GetUserId();
                var direccion = await _repository.ObtenerPorUuidAsync(idUsuario, uuid);

                if (direccion == null)
                {
                    return ApiResponse<bool>.Error("404", "La dirección no existe.");
                }

                var existeEstado = await _repository.ExisteEstadoAsync(dto.IdEstado);
                if (!existeEstado)
                {
                    return ApiResponse<bool>.Error("400", "El estado seleccionado no es válido.");
                }

                var existeMunicipio = await _repository.ExisteMunicipioAsync(dto.IdMunicipio);
                if (!existeMunicipio)
                {
                    return ApiResponse<bool>.Error("400", "El municipio seleccionado no es válido.");
                }

                if (dto.EsPredeterminada)
                {
                    await _repository.QuitarPredeterminadasAsync(idUsuario, direccion.Id);
                }

                var esPredeterminada = direccion.EsPredeterminada ? true : dto.EsPredeterminada;

                direccion.Alias = dto.Alias.Trim();
                direccion.Calle = dto.Calle.Trim();
                direccion.NumeroExterior = dto.NumeroExterior.Trim();
                direccion.NumeroInterior = string.IsNullOrWhiteSpace(dto.NumeroInterior) ? null : dto.NumeroInterior.Trim();
                direccion.Colonia = dto.Colonia.Trim();
                direccion.CodigoPostal = dto.CodigoPostal.Trim();
                direccion.IdEstado = dto.IdEstado;
                direccion.IdMunicipio = dto.IdMunicipio;
                direccion.Latitud = dto.Latitud;
                direccion.Longitud = dto.Longitud;
                direccion.Referencias = string.IsNullOrWhiteSpace(dto.Referencias) ? null : dto.Referencias.Trim();
                direccion.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
                direccion.EsPredeterminada = esPredeterminada;
                direccion.FechaActualizacion = DateTime.UtcNow;

                await _repository.ActualizarAsync(direccion);

                return ApiResponse<bool>.Success(true, "Dirección actualizada correctamente.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<bool>.Error("401", ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Error("500", $"Ocurrió un error al actualizar la dirección: {ex.Message}");
            }
        }

        // ============================================================
        // ELIMINAR
        // ============================================================

        public async Task<ApiResponse<bool>> Eliminar(Guid uuid)
        {
            try
            {
                if (!_jwtContext.EsCliente())
                {
                    return ApiResponse<bool>.Error("403", "No tienes autorización para eliminar direcciones.");
                }

                if (uuid == Guid.Empty)
                {
                    return ApiResponse<bool>.Error("400", "La dirección es requerida.");
                }

                var idUsuario = _jwtContext.GetUserId();
                var direccion = await _repository.ObtenerPorUuidAsync(idUsuario, uuid);

                if (direccion == null)
                {
                    return ApiResponse<bool>.Error("404", "La dirección no existe.");
                }

                var eraPredeterminada = direccion.EsPredeterminada;

                direccion.Activo = false;
                direccion.Eliminado = true;
                direccion.EsPredeterminada = false;
                direccion.FechaActualizacion = DateTime.UtcNow;
                direccion.FechaEliminado = DateTime.UtcNow;

                await _repository.ActualizarAsync(direccion);

                if (eraPredeterminada)
                {
                    var otraDireccion = await _repository.ObtenerPrimeraActivaAsync(idUsuario, direccion.Id);
                    if (otraDireccion != null)
                    {
                        otraDireccion.EsPredeterminada = true;
                        otraDireccion.FechaActualizacion = DateTime.UtcNow;
                        await _repository.ActualizarAsync(otraDireccion);
                    }
                }

                return ApiResponse<bool>.Success(true, "Dirección eliminada correctamente.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<bool>.Error("401", ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Error("500", $"Ocurrió un error al eliminar la dirección: {ex.Message}");
            }
        }

        // ============================================================
        // ESTABLECER PREDETERMINADA
        // ============================================================

        public async Task<ApiResponse<bool>> EstablecerPredeterminada(Guid uuid)
        {
            try
            {
                if (!_jwtContext.EsCliente())
                {
                    return ApiResponse<bool>.Error("403", "No tienes autorización para modificar direcciones.");
                }

                if (uuid == Guid.Empty)
                {
                    return ApiResponse<bool>.Error("400", "La dirección es requerida.");
                }

                var idUsuario = _jwtContext.GetUserId();
                var direccion = await _repository.ObtenerPorUuidAsync(idUsuario, uuid);

                if (direccion == null)
                {
                    return ApiResponse<bool>.Error("404", "La dirección no existe.");
                }

                if (!direccion.Activo)
                {
                    return ApiResponse<bool>.Error("400", "No puedes establecer como predeterminada una dirección inactiva.");
                }

                if (direccion.EsPredeterminada)
                {
                    return ApiResponse<bool>.Success(true, "La dirección ya es la predeterminada.");
                }

                await _repository.QuitarPredeterminadasAsync(idUsuario, direccion.Id);

                direccion.EsPredeterminada = true;
                direccion.FechaActualizacion = DateTime.UtcNow;

                await _repository.ActualizarAsync(direccion);

                return ApiResponse<bool>.Success(true, "Dirección predeterminada actualizada correctamente.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<bool>.Error("401", ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Error("500", $"Ocurrió un error al establecer la dirección predeterminada: {ex.Message}");
            }
        }

        // ============================================================
        // VALIDACIÓN
        // ============================================================

        private static string? ValidarDireccion(
            string? alias,
            string? calle,
            string? numeroExterior,
            string? colonia,
            string? codigoPostal,
            int idEstado,
            int idMunicipio,
            decimal? latitud,
            decimal? longitud)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return "El alias de la dirección es requerido.";
            }

            if (string.IsNullOrWhiteSpace(calle))
            {
                return "La calle es requerida.";
            }

            if (string.IsNullOrWhiteSpace(numeroExterior))
            {
                return "El número exterior es requerido.";
            }

            if (string.IsNullOrWhiteSpace(colonia))
            {
                return "La colonia es requerida.";
            }

            if (string.IsNullOrWhiteSpace(codigoPostal))
            {
                return "El código postal es requerido.";
            }

            if (idEstado <= 0)
            {
                return "El estado es requerido.";
            }

            if (idMunicipio <= 0)
            {
                return "El municipio es requerido.";
            }

            if (latitud.HasValue != longitud.HasValue)
            {
                return "Debes proporcionar latitud y longitud juntas.";
            }

            if (latitud.HasValue && (latitud.Value < -90 || latitud.Value > 90))
            {
                return "La latitud no es válida.";
            }

            if (longitud.HasValue && (longitud.Value < -180 || longitud.Value > 180))
            {
                return "La longitud no es válida.";
            }

            return null;
        }
    }
}
