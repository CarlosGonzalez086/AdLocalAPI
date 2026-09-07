using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<bool> ExistePorCorreoAsync(string correo);
        Task<Usuario?> GetByCorreoAsync(string correo);
        Task<ApiResponse<PagedResponse<Models.Usuario>>> GetAllAsync(int page, int pageSize, string orderBy, string search);
        Task<Usuario?> GetByIdAsync(long id);
        Task<Usuario?> GetByIdComercioAsync(long id);
        Task<Usuario?> GetByIdComercioAndIdUserAsync(long idUser, long idComercio);
        Task<Usuario?> GetByCodeAsync(string code);
        Task<Usuario?> GetByTokenAsync(string token);
        Task<Usuario> CreateAsync(Usuario usuario);
        Task UpdateAsync(Usuario usuario);
        Task DeleteAsync(int id);
        Task<string> UploadImageAsync(byte[] imageBytes, long userId, string contentType = "image/png");
        Task<bool> DeleteFromS3Async(string storageReference);
        Task UpdateUserPhotoUrlAsync(long userId, string url);
        Task DeleteColaboradorAsync(long idUser, long idComercio);
        Task<Usuario?> GetByCodigoReferidoAsync(string codigo);
        Task<Usuario?> GetByStripeId(string CustomerId);
    }
}
