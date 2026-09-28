using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Services
{
    public interface ICalificacionComentarioService
    {
        Task<ApiResponse<object>> CrearComentario(CalificacionComentarioCreateDto dto);
        Task<ApiResponse<object>> ObtenerComentarios(long idComercio, int page = 1, int pageSize = 10, string orderBy = "desc");
    }
}
