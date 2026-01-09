using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Utils;
using Stripe;
using Stripe.Checkout;
using Supabase.Gotrue;

namespace AdLocalAPI.Services
{
    public class StripeService : IStripeService
    {

        private readonly StripeSettings _stripeSettings;
        private readonly UsuarioRepository _usuarioRepository;

        public StripeService(StripeSettings stripeSettings, UsuarioRepository usuarioRepository)
        {
            _stripeSettings = stripeSettings;
            _usuarioRepository = usuarioRepository;

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

        public async Task<PaymentIntent> CreatePaymentIntent(
    Models.Plan plan,
    int usuarioId,
    string paymentMethodId
)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
            if (string.IsNullOrEmpty(usuario.StripeCustomerId))
            {
                var customerId = await CreateCustomer(usuario.Email);
                usuario.StripeCustomerId = customerId;
                await _usuarioRepository.UpdateAsync(usuario);
            }
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(plan.Precio * 100),
                Currency = "mxn",
                Customer = usuario.StripeCustomerId,
                PaymentMethod = paymentMethodId,
                Confirm = true,
                OffSession = true,
                Metadata = new Dictionary<string, string>
        {
            { "usuarioId", usuarioId.ToString() },
            { "planId", plan.Id.ToString() }
        }
            };

            var service = new PaymentIntentService();
            return await service.CreateAsync(options);
        }



    }
}
