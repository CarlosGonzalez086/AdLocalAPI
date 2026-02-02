//using AdLocalAPI.Models;
//using AdLocalAPI.Repositories;
//using Stripe;

//namespace AdLocalAPI.Services
//{
//    public class SuscripcionAutoRenewService
//    {
//        private readonly SuscripcionRepository _suscripcionRepo;
//        private readonly TarjetaRepository _tarjetaRepository;
//        private readonly IConfiguration _config;

//        public SuscripcionAutoRenewService(
//            SuscripcionRepository suscripcionRepo,
//            TarjetaRepository tarjetaRepository,
//            IConfiguration config)
//        {
//            _suscripcionRepo = suscripcionRepo;
//            _tarjetaRepository = tarjetaRepository;
//            _config = config;
//        }
//        public async Task ProcesarAutoRenovacionesAsync()
//        {
//            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
//            var hoy = DateTime.UtcNow;

//            var suscripciones = await _suscripcionRepo
//                .ObtenerParaAutoRenovacionAsync(hoy);
//            foreach (var suscripcion in suscripciones)
//            {
//                var cardUser = await _tarjetaRepository.GetByUser(suscripcion.UsuarioId);
//                var pagoExitoso = await CobrarRenovacionAsync(suscripcion,cardUser.);

//                if (!pagoExitoso)
//                    continue;

//                ActualizarPeriodoSuscripcion(suscripcion, hoy);

//                await _suscripcionRepo.ActualizarAsync(suscripcion);
//            }
//        }

//        private static void ActualizarPeriodoSuscripcion(
//            Suscripcion suscripcion,
//            DateTime fechaBase)
//        {
//            suscripcion.FechaInicio = fechaBase;
//            suscripcion.FechaFin = fechaBase.AddMonths(1);
//            suscripcion.Estado = "activa";
//            suscripcion.Activa = true;
//        }

//        private async Task<bool> CobrarRenovacionAsync(Suscripcion suscripcion)
//        {
//            try
//            {
//                var paymentService = new PaymentIntentService();

//                var intent = await paymentService.CreateAsync(
//                    new PaymentIntentCreateOptions
//                    {
//                        Amount = (long)(suscripcion.Plan.Precio * 100),
//                        Currency = "mxn",
//                        Customer = suscripcion.Usuario.StripeCustomerId,
//                        OffSession = true,
//                        Confirm = true,
//                        PaymentMethodTypes = new List<string> { "card" },
//                        PaymentMethod = suscripcion.,
//                        Metadata = new Dictionary<string, string>
//                        {
//                            { "usuarioId", suscripcion.UsuarioId.ToString() },
//                            { "planId", suscripcion.PlanId.ToString() },
//                            { "planNombre", suscripcion.Plan.Nombre },
//                            { "autoRenew", suscripcion.AutoRenovacion ? "si" : "no" }
//                        }
//                    });

//                return intent.Status == "succeeded";
//            }
//            catch (StripeException)
//            {
//                return false;
//            }
//        }

//    }
//}