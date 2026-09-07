using System.Threading.Tasks;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IStripeWebhookEventRepository
    {
        Task<StripeWebhookEvent?> ObtenerPorEventIdAsync(string stripeEventId);
        Task CrearAsync(StripeWebhookEvent evento);
        Task ActualizarAsync(StripeWebhookEvent evento);
        Task<IDbContextTransaction> IniciarTransaccionAsync();
    }
}
