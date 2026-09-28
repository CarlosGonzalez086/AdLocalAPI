using System.Threading.Tasks;
using static AdLocalAPI.DTOs.ComercioVisitaDTOs;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IComercioVisitaRepository
    {
        Task<bool> RegistrarVisitaUnica(long comercioId, string? ip);
        Task<ComercioVisitasStatsDto> GetStats(long comercioId);
    }
}
