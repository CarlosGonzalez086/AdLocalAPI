using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace AdLocalAPI.Services
{
    public class SuscripcionAutoRenewService
    {
            private readonly AppDbContext _context;
            private readonly IConfiguration _config;

            public SuscripcionAutoRenewService(
                AppDbContext context,
                IConfiguration config)
            {
                _context = context;
                _config = config;
            }
            public async Task ProcesarAutoRenovaciones()
            {
                StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
                var hoy = DateTime.UtcNow;

                var suscripciones = await _context.Suscripcions
                    .Include(s => s.Usuario)
                    .Include(s => s.Plan)
                    .Where(s =>
                        s.Activa &&
                        s.AutoRenovacion &&
                        s.FechaFin <= hoy &&
                        s.Usuario.StripeCustomerId != null &&
                        s.StripeCustomerId != null
                    )
                    .ToListAsync();

                foreach (var suscripcion in suscripciones)
                {
                    var pagoExitoso = await CobrarRenovacion(suscripcion);

                    if (!pagoExitoso)
                        continue;

                    suscripcion.FechaInicio = hoy;
                    suscripcion.FechaFin = hoy.AddMonths(1);
                    suscripcion.Estado = "activa";
                    suscripcion.Activa = true;
                }

                await _context.SaveChangesAsync();
            }
        private async Task<bool> CobrarRenovacion(Suscripcion suscripcion)
        {
            try
            {
                var paymentService = new PaymentIntentService();

                var intent = await paymentService.CreateAsync(
                    new PaymentIntentCreateOptions
                    {
                        Amount = (long)(suscripcion.Plan.Precio * 100),
                        Currency = "mxn",
                        Customer = suscripcion.Usuario.StripeCustomerId,
                        PaymentMethod = "stripe",
                        OffSession = true,
                        Confirm = true,
                        PaymentMethodTypes = new List<string> { "card" },
                        Metadata = new Dictionary<string, string>
                        {
                            { "usuarioId", suscripcion.UsuarioId.ToString() },
                            { "planId", suscripcion.PlanId.ToString() },
                            { "plan_nombre", suscripcion.Plan.Nombre },
                            { "autoRenew", suscripcion.AutoRenovacion ? "si" : "no" },
                        },
                    });

                return intent.Status == "succeeded";
            }
            catch (StripeException)
            {
                return false;
            }
        }
    }
}