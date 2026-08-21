using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.Direcciones;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IDireccionUsuarioService
    {
        Task<ApiResponse<IEnumerable<DireccionUsuarioResponseDto>>>
            ObtenerTodas();

        Task<ApiResponse<DireccionUsuarioResponseDto>>
            ObtenerPorUuid(Guid uuid);

        Task<ApiResponse<DireccionUsuarioResponseDto>>
            Crear(DireccionUsuarioDto dto);

        Task<ApiResponse<bool>>
            Actualizar(
                Guid uuid,
                DireccionUsuarioDto dto
            );

        Task<ApiResponse<bool>>
            Eliminar(Guid uuid);

        Task<ApiResponse<bool>>
            EstablecerPredeterminada(Guid uuid);
    }
}