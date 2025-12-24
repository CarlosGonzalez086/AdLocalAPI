using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using AdLocalAPI.Data;
using AdLocalAPI.Repositories;
using AdLocalAPI.Models;

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
            var json = await new StreamReader(Request.Body).ReadToEndAsync();

            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _config["Stripe:WebhookSecret"]
                );
            }
            catch (Exception)
            {
                return BadRequest();
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                int usuarioId = int.Parse(session.Metadata["usuarioId"]);
                int planId = int.Parse(session.Metadata["planId"]);

                var plan = await _planRepo.GetByIdAsync(planId);

                if (plan != null)
                {
                    await _suscripcionRepo.CreateAsync(new Suscripcion
                    {
                        UsuarioId = usuarioId,
                        PlanId = planId,
                        FechaInicio = DateTime.UtcNow,
                        FechaFin = DateTime.UtcNow.AddDays(plan.DuracionDias),
                        Activa = true,
                        StripeSessionId = session.Id
                    });
                }
            }

            return Ok();
        }
        [HttpPost("test-checkout")]
        public async Task<IActionResult> CreateTestCheckout()
        {
            var options = new SessionCreateOptions
            {
                Mode = "payment", // pago único (para pruebas)
                PaymentMethodTypes = new List<string> { "card" },

                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "mxn",
                            UnitAmount = 10000, // $100.00 MXN
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
