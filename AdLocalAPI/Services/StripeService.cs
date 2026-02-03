using AdLocalAPI.Interfaces;
using Stripe;

namespace AdLocalAPI.Services
{
    public class StripeService : IStripeService
    {
        // ❌ NO constructor con keys
        public StripeService()
        {
        }

        // =========================
        // CUSTOMER
        // =========================
        public async Task<string> CreateCustomer(string email)
        {
            var service = new CustomerService();

            var customer = await service.CreateAsync(new CustomerCreateOptions
            {
                Email = email
            });

            return customer.Id;
        }

        // =========================
        // PAYMENT METHODS
        // =========================
        public async Task<PaymentMethod> GetPaymentMethod(string paymentMethodId)
        {
            return await new PaymentMethodService().GetAsync(paymentMethodId);
        }

        public async Task AttachToCustomer(string paymentMethodId, string customerId)
        {
            await new PaymentMethodService().AttachAsync(
                paymentMethodId,
                new PaymentMethodAttachOptions
                {
                    Customer = customerId
                }
            );
        }

        public async Task Detach(string paymentMethodId)
        {
            await new PaymentMethodService().DetachAsync(paymentMethodId);
        }

        public async Task SetDefaultPaymentMethod(string customerId, string paymentMethodId)
        {
            await new CustomerService().UpdateAsync(
                customerId,
                new CustomerUpdateOptions
                {
                    InvoiceSettings = new CustomerInvoiceSettingsOptions
                    {
                        DefaultPaymentMethod = paymentMethodId
                    }
                }
            );
        }

        // =========================
        // SUBSCRIPTIONS
        // =========================
        public async Task<Subscription> CreateSubscription(
            string customerId,
            string priceId,
            string paymentMethodId
        )
        {
            await AttachToCustomer(paymentMethodId, customerId);
            await SetDefaultPaymentMethod(customerId, paymentMethodId);

            var subscription = await new SubscriptionService().CreateAsync(
                new SubscriptionCreateOptions
                {
                    Customer = customerId,
                    Items = new()
                    {
                        new SubscriptionItemOptions
                        {
                            Price = priceId
                        }
                    },
                    DefaultPaymentMethod = paymentMethodId,
                    PaymentSettings = new SubscriptionPaymentSettingsOptions
                    {
                        SaveDefaultPaymentMethod = "on_subscription"
                    },
                    Expand = new List<string>
                    {
                        "latest_invoice.payment_intent"
                    }
                }
            );

            return subscription;
        }

        public async Task CancelSubscription(string subscriptionId, bool atPeriodEnd = true)
        {
            var service = new SubscriptionService();

            if (atPeriodEnd)
            {
                await service.UpdateAsync(
                    subscriptionId,
                    new SubscriptionUpdateOptions
                    {
                        CancelAtPeriodEnd = true
                    }
                );
            }
            else
            {
                await service.CancelAsync(subscriptionId);
            }
        }

        public async Task ChangePlan(
            string subscriptionId,
            string subscriptionItemId,
            string newPriceId
        )
        {
            await new SubscriptionService().UpdateAsync(
                subscriptionId,
                new SubscriptionUpdateOptions
                {
                    Items = new()
                    {
                        new SubscriptionItemOptions
                        {
                            Id = subscriptionItemId,
                            Price = newPriceId
                        }
                    },
                    ProrationBehavior = "create_prorations"
                }
            );
        }

        // =========================
        // SETUP INTENT
        // =========================
        public async Task<string> CrearSetupIntent(string stripeCustomerId)
        {
            var service = new SetupIntentService();

            var setupIntent = await service.CreateAsync(
                new SetupIntentCreateOptions
                {
                    Customer = stripeCustomerId,
                    PaymentMethodTypes = new List<string> { "card" }
                }
            );

            return setupIntent.ClientSecret;
        }
    }
}
