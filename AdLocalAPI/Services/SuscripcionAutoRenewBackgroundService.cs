using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AdLocalAPI.Services
{
    public class SuscripcionAutoRenewBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SuscripcionAutoRenewBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var ahora = DateTime.UtcNow;

                var proximaEjecucion = ahora.Date.AddHours(10);

                if (ahora > proximaEjecucion)
                    proximaEjecucion = proximaEjecucion.AddDays(1);

                var delay = proximaEjecucion - ahora;

                if (delay.TotalMilliseconds > 0)
                    await Task.Delay(delay, stoppingToken);

                using var scope = _scopeFactory.CreateScope();

                var autoRenewService = scope.ServiceProvider
                    .GetRequiredService<SuscripcionAutoRenewService>();

                await autoRenewService.ProcesarAutoRenovaciones();
            }
        }
    }
}
