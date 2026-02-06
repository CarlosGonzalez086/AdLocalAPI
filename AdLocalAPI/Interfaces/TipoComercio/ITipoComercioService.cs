using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.TipoComercio
{
    public interface ITipoComercioService
    {
        Task<ApiResponse<object>> Crear(TipoComercioCreateDto dto);
        Task<ApiResponse<object>> Actualizar(long id, TipoComercioCreateDto dto);
        Task<ApiResponse<bool>> Eliminar(long id);
        Task<ApiResponse<TipoComercioDto>> GetById(long id);
        Task<ApiResponse<PagedResponse<TipoComercioDto>>> GetAllPagedAsync(
                    int page = 1,
                    int pageSize = 10,
                    string orderBy = "recent",
                    string search = ""
                );
        Task<ApiResponse<List<TipoComercioDto>>> GetAllForSelectAsync();
    }
}
