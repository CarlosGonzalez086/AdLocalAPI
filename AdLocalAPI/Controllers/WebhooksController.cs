using AdLocalAPI.Data;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly SuscripcionRepository _suscripcionRepo;
        private readonly PlanRepository _planRepo;

        public WebhooksController(
            IConfiguration config,
            SuscripcionRepository suscripcionRepo,
            PlanRepository planRepo)
        {
            _config = config;
            _suscripcionRepo = suscripcionRepo;
            _planRepo = planRepo;
        }

        /// <summary>
        /// Stripe Webhook
        /// </summary>
        [HttpPost("stripe")]
        public async Task<IActionResult> StripeWebhook()
        {
            Request.EnableBuffering();

            var json = await new StreamReader(Request.Body).ReadToEndAsync();
            Request.Body.Position = 0;

            var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
            if (string.IsNullOrEmpty(webhookSecret))
                return StatusCode(500);

            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    webhookSecret
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("Firma inválida: " + ex.Message);
                return BadRequest();
            }

            if (stripeEvent.Type == Stripe.EventTypes.PaymentIntentSucceeded)
            {
                var intent = stripeEvent.Data.Object as PaymentIntent;
                if (intent == null) return Ok();

                int usuarioId = int.Parse(intent.Metadata["usuarioId"]);
                int planId = int.Parse(intent.Metadata["planId"]);

                var existe = await _suscripcionRepo.ExistePorSessionAsync(intent.Id);
                if (existe) return Ok();

                var plan = await _planRepo.GetByIdAsync(planId);
                if (plan == null) return Ok();

                await _suscripcionRepo.CreateAsync(new Suscripcion
                {
                    UsuarioId = usuarioId,
                    PlanId = planId,

                    StripeCustomerId = intent.CustomerId,
                    StripeSessionId = "",
                    StripeSubscriptionId = "",
                    StripePriceId = "",
                    

                    Monto = intent.AmountReceived / 100m,
                    Moneda = intent.Currency.ToUpper(),
                    MetodoPago = "stripe",

                    FechaInicio = DateTime.UtcNow,
                    FechaFin = DateTime.UtcNow.AddDays(plan.DuracionDias),

                    Activa = true,
                    Estado = "active",
                    AutoRenovacion = true,
                    Eliminada = false,
                    FechaCreacion = DateTime.UtcNow
                });
            }


            return Ok();
        }


        [HttpPost("test-checkout")]
        public async Task<IActionResult> CreateTestCheckout()
        {
            var options = new SessionCreateOptions
            {
                Mode = "payment", 
                PaymentMethodTypes = new List<string> { "card" },

                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "mxn",
                            UnitAmount = 10000, 
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Plan de Prueba"
                            }
                        }
                    }
                },

                SuccessUrl = "http://localhost:5173/pago-exitoso",
                CancelUrl = "http://localhost:5173/pago-cancelado",

                Metadata = new Dictionary<string, string>
                {
                    { "usuarioId", "1" },
                    { "planId", "1" }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return Ok(new
            {
                sessionId = session.Id,
                checkoutUrl = session.Url
            });
        }
    }
}
