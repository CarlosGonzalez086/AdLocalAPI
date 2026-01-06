using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;
using Stripe;
using Stripe.Checkout;

namespace AdLocalAPI.Services
{
    public class StripeService : IStripeService
    {

        private readonly StripeSettings _stripeSettings;

        public StripeService(StripeSettings stripeSettings)
        {
            _stripeSettings = stripeSettings;

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        public async Task<PaymentMethod> GetPaymentMethod(string id)
        {
            var service = new PaymentMethodService();
            return await service.GetAsync(id);
        }

        public async Task AttachToCustomer(string pmId, string customerId)
        {
            var service = new PaymentMethodService();
            await service.AttachAsync(pmId, new PaymentMethodAttachOptions
            {
                Customer = customerId
            });
        }

        public async Task Detach(string pmId)
        {
            var service = new PaymentMethodService();
            await service.DetachAsync(pmId);
        }

        public async Task SetDefault(string customerId, string pmId)
        {
            var service = new CustomerService();
            await service.UpdateAsync(customerId, new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = pmId
                }
            });
        }

        public async Task<string?> CreateCustomer(string email)
        {
            try
            {
                var service = new CustomerService();

                var customer = await service.CreateAsync(new CustomerCreateOptions
                {
                    Email = email
                });

                return customer.Id;
            }
            catch (StripeException ex)
            {
                Console.WriteLine($"Error al crear el cliente en Stripe: {ex.Message}");
                return null; 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado al crear el cliente: {ex.Message}");
                return null;
            }
        }


        public Session CreateCheckoutSession(Models.Plan plan, int usuarioId)
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
