using AdLocalAPI.Models;
using Stripe.Checkout;

namespace AdLocalAPI.Services
{
    public class StripeService
    {
        public StripeService(IConfiguration config)
        {
            Stripe.StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
        }

        public Session CreateCheckoutSession(Plan plan, int usuarioId)
        {
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "mxn",
                        UnitAmount = (long)(plan.Precio * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = plan.Nombre
                        }
                    },
                    Quantity = 1
                }
            },
                SuccessUrl = $"https://tu-frontend.com/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"https://tu-frontend.com/cancel",
                Metadata = new Dictionary<string, string>
            {
                { "usuarioId", usuarioId.ToString() },
                { "planId", plan.Id.ToString() }
            }
            };

            var service = new SessionService();
            return service.Create(options);
        }
    }
}
