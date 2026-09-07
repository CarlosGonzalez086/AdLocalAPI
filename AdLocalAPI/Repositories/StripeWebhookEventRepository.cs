using System.Threading.Tasks;
using AdLocalAPI.Data;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AdLocalAPI.Repositories
{
    public class StripeWebhookEventRepository : IStripeWebhookEventRepository
    {
        private readonly AppDbContext _context;

        public StripeWebhookEventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StripeWebhookEvent?> ObtenerPorEventIdAsync(string stripeEventId)
        {
            return await _context.StripeWebhookEvents
                .FirstOrDefaultAsync(e => e.StripeEventId == stripeEventId);
        }

        public async Task CrearAsync(StripeWebhookEvent evento)
        {
            _context.StripeWebhookEvents.Add(evento);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(StripeWebhookEvent evento)
        {
            _context.StripeWebhookEvents.Update(evento);
            await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> IniciarTransaccionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}
