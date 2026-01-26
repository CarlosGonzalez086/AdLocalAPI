using Stripe;

namespace AdLocalAPI.Services
{
    public class StripeServiceSub
    {
        public StripeServiceSub(IConfiguration config)
        {
            StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
        }

        public Customer CrearCliente(string email)
        {
            return new CustomerService().Create(new CustomerCreateOptions
            {
                Email = email
            });
        }

        public Subscription CambiarPlan(string subscriptionId, string priceId)
        {
            var service = new SubscriptionService();
            var subscription = service.Get(subscriptionId);

            return service.Update(subscriptionId, new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Id = subscription.Items.Data[0].Id,
                        Price = priceId
                    }
                },
                ProrationBehavior = "create_prorations"
            });
        }

        public void CancelarSuscripcion(string subscriptionId)
        {
            new SubscriptionService().Cancel(subscriptionId);
        }
    }
}
