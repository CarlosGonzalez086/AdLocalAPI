using System.Threading.Tasks;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Services
{
    public interface IBeneficiosService
    {
        Task<ApiResponse<object>> ReclamarBeneficio();
    }
}
