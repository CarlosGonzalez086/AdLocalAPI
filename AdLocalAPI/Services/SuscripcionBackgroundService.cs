namespace AdLocalAPI.Services
{
    public class SuscripcionBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SuscripcionBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var ahora = DateTime.Now;

                DateTime proximaEjecucion;

                if (ahora.Hour < 12)
                {
                    proximaEjecucion = ahora.Date.AddHours(12);
                }
                else
                {
                    proximaEjecucion = ahora.Date.AddDays(1);
                }

                var delay = proximaEjecucion - ahora;

                if (delay.TotalMilliseconds > 0)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<SuscripcionServiceAuto>();

                await service.ProcesarSuscripcionesVencidas();
            }
        }
    }
}
