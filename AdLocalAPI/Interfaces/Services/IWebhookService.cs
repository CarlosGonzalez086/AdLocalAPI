using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Services
{
    public interface IWebhookService
    {
        Task<ApiResponse<object>> ProcesarWebhookStripeAsync(string jsonPayload, string? signatureHeader, CancellationToken cancellationToken = default);
    }
}
