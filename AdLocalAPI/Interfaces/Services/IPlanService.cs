using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Services
{
    public interface IPlanService
    {
        Task<ApiResponse<object>> GetAllPlanes(int page, int pageSize, string orderBy, string search);
        Task<ApiResponse<object>> GetAllPlanesUser();
        Task<ApiResponse<object>> GetPlanById(int id);
        Task<ApiResponse<object>> CrearPlan(PlanCreateDto dto);
        Task<ApiResponse<object>> ActualizarPlan(int id, PlanCreateDto dto);
        Task<ApiResponse<object>> EliminarPlan(int id);
    }
}
