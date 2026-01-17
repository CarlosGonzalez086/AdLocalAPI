using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Location
{
    public interface ILocationService
    {
        Task<ApiResponse<List<StateDto>>> GetAllStatesAsync();
        Task<ApiResponse<List<MunicipalityDto>>> GetMunicipalitiesByStateIdAsync(int stateId);
    }
}
