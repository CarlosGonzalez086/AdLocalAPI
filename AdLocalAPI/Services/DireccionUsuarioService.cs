using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.Direcciones;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;

namespace AdLocalAPI.Services
{
    public partial class DireccionUsuarioService : IDireccionUsuarioService
    {
        private readonly IDireccionUsuarioRepository _repository;
        private readonly JwtContext _jwtContext;

        public DireccionUsuarioService(
            IDireccionUsuarioRepository repository,
            JwtContext jwtContext)
        {
            _repository = repository;
            _jwtContext = jwtContext;
        }

        // ============================================================
        // OBTENER TODAS
        // ============================================================

        public async Task<ApiResponse<IEnumerable<DireccionUsuarioResponseDto>>> ObtenerTodas()
        {
            try
            {
                if (!_jwtContext.EsCliente())
                {
                    return ApiResponse<IEnumerable<DireccionUsuarioResponseDto>>.Error(
                        "403",
                        "No tienes autorización para consultar direcciones."
                    );
                }

                var idUsuario = _jwtContext.GetUserId();
                var direcciones = await _repository.ObtenerTodasAsync(idUsuario);
                var result = direcciones.Select(MapearResponse);

                return ApiResponse<IEnumerable<DireccionUsuarioResponseDto>>.Success(
                    result,
                    "Direcciones obtenidas correctamente."
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<IEnumerable<DireccionUsuarioResponseDto>>.Error("401", ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<DireccionUsuarioResponseDto>>.Error(
                    "500",
                    $"Ocurrió un error al consultar las direcciones: {ex.Message}"
                );
            }
        }

        // ============================================================
        // OBTENER POR UUID
        // ============================================================

        public async Task<ApiResponse<DireccionUsuarioResponseDto>> ObtenerPorUuid(Guid uuid)
        {
            try
            {
                if (!_jwtContext.EsCliente())
                {
                    return ApiResponse<DireccionUsuarioResponseDto>.Error(
                        "403",
                        "No tienes autorización para consultar esta dirección."
                    );
                }

                if (uuid == Guid.Empty)
                {
                    return ApiResponse<DireccionUsuarioResponseDto>.Error("400", "La dirección es requerida.");
                }

                var idUsuario = _jwtContext.GetUserId();
                var direccion = await _repository.ObtenerPorUuidAsync(idUsuario, uuid);

                if (direccion == null)
                {
                    return ApiResponse<DireccionUsuarioResponseDto>.Error("404", "La dirección no existe.");
                }

                return ApiResponse<DireccionUsuarioResponseDto>.Success(
                    MapearResponse(direccion),
                    "Dirección obtenida correctamente."
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
                    $"Ocurrió un error al consultar la dirección: {ex.Message}"
                );
            }
        }

        // ============================================================
        // MAPEO
        // ============================================================

        private static DireccionUsuarioResponseDto MapearResponse(DireccionUsuario direccion)
        {
            return new DireccionUsuarioResponseDto
            {
                Uuid = direccion.Uuid,
                Alias = direccion.Alias,
                Calle = direccion.Calle,
                NumeroExterior = direccion.NumeroExterior,
                NumeroInterior = direccion.NumeroInterior,
                Colonia = direccion.Colonia,
                CodigoPostal = direccion.CodigoPostal,
                IdEstado = direccion.IdEstado,
                Estado = direccion.Estado?.EstadoNombre ?? string.Empty,
                IdMunicipio = direccion.IdMunicipio,
                Municipio = direccion.Municipio?.MunicipioNombre ?? string.Empty,
                Latitud = direccion.Latitud,
                Longitud = direccion.Longitud,
                Referencias = direccion.Referencias,
                Telefono = direccion.Telefono,
                EsPredeterminada = direccion.EsPredeterminada,
                Activo = direccion.Activo,
                FechaCreacion = direccion.FechaCreacion,
                FechaActualizacion = direccion.FechaActualizacion
            };
        }
    }
}