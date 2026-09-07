using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces
{
    public interface IStripeReconciliationService
    {
        Task<ApiResponse<SuscripcionInfoDto>> ReconciliarUsuarioAsync(long usuarioId, CancellationToken cancellationToken = default);
        Task<ApiResponse<ReconciliacionResumenDto>> ReconciliarTodasAsync(CancellationToken cancellationToken = default);
    }
}
