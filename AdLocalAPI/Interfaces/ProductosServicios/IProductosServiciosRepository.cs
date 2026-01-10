using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.ProductosServicios
{
    public interface IProductosServiciosRepository
    {
        Task<Models.ProductosServicios> CreateAsync(Models.ProductosServicios entity);
        Task UpdateAsync(Models.ProductosServicios entity);
        Task<Models.ProductosServicios?> GetByIdAsync(long id,long idComercio, long idUser);
        Task<IEnumerable<Models.ProductosServicios>> GetAllAsync(long idComercio);
        Task<ApiResponse<PagedResponse<ProductosServiciosDto>>> GetAllPagedAsync(
   long idUser, long idComercio, int page = 1, int pageSize = 10, string orderBy = "recent", string search = "");
        Task<string> UploadToSupabaseAsync(byte[] imageBytes,int userId,string contentType = "image/png");
        Task<bool> DeleteFromSupabaseByUrlAsync(string publicUrl);
    }
}
