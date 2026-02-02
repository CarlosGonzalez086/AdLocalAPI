using Stripe;

namespace AdLocalAPI.Interfaces
{
    public interface IStripeService
    {
 
        Task<string> CreateCustomer(string email);

        Task<PaymentMethod> GetPaymentMethod(string paymentMethodId);
        Task AttachToCustomer(string paymentMethodId, string customerId);
        Task Detach(string paymentMethodId);
        Task SetDefaultPaymentMethod(string customerId, string paymentMethodId);
        Task<Subscription> CreateSubscription(
            string customerId,
            string priceId,
            string paymentMethodId
        );
        Task CancelSubscription(string subscriptionId, bool atPeriodEnd = true);
        Task ChangePlan(string subscriptionId, string subscriptionItemId, string newPriceId);
    }
}
