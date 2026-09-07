using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IPlanRepository
    {
        Task<object> GetAllAsync(int page, int pageSize, string orderBy, string search);
        Task<List<Plan>> GetAllPlanesUser();
        Task<Plan?> GetByIdAsync(int id);
        Task<Plan?> GetByIdLongAsync(long id);
        Task<Plan> CreateAsync(Plan plan);
        Task<Plan> UpdateAsync(Plan plan);
        Task<bool> DeleteAsync(int id);
        Task<Plan?> GetByTipoAsync(string tipo);
        Task<Plan?> GetByStripePriceIdAsync(string priceId);
    }
}
