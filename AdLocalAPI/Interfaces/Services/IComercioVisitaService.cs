using System.Threading.Tasks;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Services
{
    public interface IComercioVisitaService
    {
        Task<ApiResponse<object>> RegistrarVisita(long comercioId, string? ip);
        Task<ApiResponse<object>> GetStats(long comercioId);
    }
}
