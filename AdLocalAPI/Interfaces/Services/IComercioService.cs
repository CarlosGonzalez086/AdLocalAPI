using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Services
{
    public interface IComercioService
    {
        Task<ApiResponse<object>> GetAllComercios(
            string tipo,
            double lat,
            double lng,
            string municipio,
            int page,
            int pageSize,
            string ip,
            long? idCliente = null
        );

        Task<ApiResponse<ComercioMineDto>> GetComercioById(long id);
        Task<ApiResponse<ComercioMineDto>> GetComercioByUser();
        Task<ApiResponse<object>> CreateComercio(ComercioCreateDto dto);
        Task<ApiResponse<object>> UpdateComercio(ComercioUpdateDto dto);
        Task<ApiResponse<object>> DeleteComercio(long id);

        Task<ApiResponse<object>> GetByFiltros(
            int estadoId,
            int municipioId,
            long idTipoComercio,
            string orden,
            int page,
            int pageSize
        );

        Task<ApiResponse<PagedResponse<ComercioPublicDto>>> GetAllComerciosByUserPaged(
            int page = 1,
            int pageSize = 10
        );

        Task<ApiResponse<object>> GeTotalComerciosByIdUsuario();
        Task<ApiResponse<object>> guardarColaborador(ColaborarDto dto);
        Task<ApiResponse<PagedResponse<UsuarioInfoDto>>> getAllColaboradores(long idComercio, int page = 1, int pageSize = 10);
        Task<ApiResponse<bool>> toggleAccesoColaborador(int idColaborador, long idComercio);
        Task<ApiResponse<bool>> eliminarColaborador(int idColaborador, long idComercio);
    }
}
