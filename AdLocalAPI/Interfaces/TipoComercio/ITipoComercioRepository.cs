using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.TipoComercio
{
    public interface ITipoComercioRepository
    {
        Task<Models.TipoComercio> GetById(long id);
        Task<ApiResponse<PagedResponse<TipoComercioDto>>> GetAllPagedAsync(
                    int page = 1,
                    int pageSize = 10,
                    string orderBy = "recent",
                    string search = ""
                );
        Task<Models.TipoComercio> Create(Models.TipoComercio entity);
        Task Update(Models.TipoComercio entity);
        Task Delete(Models.TipoComercio entity);
        Task<List<Models.TipoComercio>> GetAllForSelect();
    }
}
