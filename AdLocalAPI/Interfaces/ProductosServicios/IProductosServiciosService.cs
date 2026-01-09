using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.ProductosServicios
{
    public interface IProductosServiciosService
    {
        Task<ApiResponse<ProductosServiciosDto>> CreateAsync(ProductosServiciosDto dto);
        Task<ApiResponse<IEnumerable<ProductosServiciosDto>>> GetAllAsync(long idComercio);
        Task<ApiResponse<ProductosServiciosDto>> GetByIdAsync(long id);
        Task<ApiResponse<bool>> UpdateAsync(long id, ProductosServiciosDto dto);
        Task<ApiResponse<bool>> DeleteAsync(long id);
        Task<ApiResponse<bool>> DesactivarAsync(long id);
        Task<ApiResponse<PagedResponse<ProductosServiciosDto>>> GetAllPagedAsync(
           int page = 1, int pageSize = 10, string orderBy = "recent", string search = "");
    }
}
