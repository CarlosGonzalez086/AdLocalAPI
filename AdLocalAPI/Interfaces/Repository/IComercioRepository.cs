using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IComercioRepository
    {
        Task<(List<ComercioPublicDto> comercios, int total)> GetAllAsync(
            string tipo, double lat, double lng,
            string municipio, int page, int pageSize, string ip, long? idCliente = null);

        Task<Comercio?> GetByIdAsync(long id);
        Task<Comercio?> GetComercioByUser(long idUser);
        Task<Comercio> CreateAsync(Comercio comercio);
        Task UpdateAsync(Comercio comercio);
        Task DeleteAsync(long id);
        Task<string> UploadImageAsync(byte[] imageBytes, long userId, string contentType = "image/png");
        Task<bool> DeleteFromS3Async(string storageReference);
        Task<(List<ComercioPublicDto> items, int total)> GetByFiltros(
            int estadoId, int municipioId, long idTipoComercio, string orden, int page, int pageSize);

        Task<ApiResponse<PagedResponse<ComercioPublicDto>>> GetAllComerciosByUserPaged(
            long idUser, int page = 1, int pageSize = 10);

        Task<List<Comercio>> GetAllComerciosByIdUsuario(long idUser);
        Task<ApiResponse<PagedResponse<UsuarioInfoDto>>> getAllColaboradores(
            long idComercio, int page = 1, int pageSize = 10);

        Task<bool> PuedeAdministrarAsync(long comercioId, long usuarioId);
    }
}
