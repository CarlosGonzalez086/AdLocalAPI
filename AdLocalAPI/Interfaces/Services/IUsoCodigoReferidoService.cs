using System.Threading.Tasks;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Services
{
    public interface IUsoCodigoReferidoService
    {
        Task<ApiResponse<int>> ContarMisUsosAsync();
        Task<ApiResponse<int>> ContarPorCodigoAsync(string codigo);
        Task<ApiResponse<int>> ContarTotalAsync();
    }
}
