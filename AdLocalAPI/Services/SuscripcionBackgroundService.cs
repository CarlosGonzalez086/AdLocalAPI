//namespace AdLocalAPI.Services
//{
//    public class SuscripcionBackgroundService : BackgroundService
//    {
//        private readonly IServiceScopeFactory _scopeFactory;

//        public SuscripcionBackgroundService(IServiceScopeFactory scopeFactory)
//        {
//            _scopeFactory = scopeFactory;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                var ahora = DateTime.UtcNow;

//                var proximaEjecucion = ahora.Date.AddHours(12);

//                if (ahora > proximaEjecucion)
//                    proximaEjecucion = proximaEjecucion.AddDays(1);

//                var delay = proximaEjecucion - ahora;

//                if (delay.TotalMilliseconds > 0)
//                    await Task.Delay(delay, stoppingToken);

//                using var scope = _scopeFactory.CreateScope();
//                var service = scope.ServiceProvider
//                    .GetRequiredService<SuscripcionAutoService>();

//                await service.SincronizarSuscripcionesAsync();
//            }
//        }
//    }
//}
