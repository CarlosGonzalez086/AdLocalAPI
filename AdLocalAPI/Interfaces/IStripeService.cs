using Stripe;

namespace AdLocalAPI.Interfaces
{
    public interface IStripeService
    {
        Task<PaymentMethod> GetPaymentMethod(string paymentMethodId);
        Task AttachToCustomer(string paymentMethodId, string customerId);
        Task Detach(string paymentMethodId);
        Task SetDefault(string customerId, string paymentMethodId);
        Task<string> CreateCustomer(string email);
    }
}
