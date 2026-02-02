using AdLocalAPI.Models;

public interface ISuscriptionServiceV1
{
    Task<ApiResponse<string>> SuscribirseConTarjeta(int planId, string paymentMethodId,bool autoRenew);
    Task<ApiResponse<string>> CrearCheckoutSuscripcion(int planId);
    Task<ApiResponse<string>> CancelarPlan();
}
