using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Location
{
    public interface ILocationRepository
    {
        Task<List<Estado>> GetAllStatesAsync();
        Task<Estado> GetStateByIdAsync(int id);
        Task<List<Municipio>> GetMunicipalitiesByStateIdAsync(int stateId);
        Task<Municipio> GetMunicipalityByIdAsync(int id);
    }
}
