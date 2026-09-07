using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces
{
    public interface ISuscripcionService
    {
        Task<ApiResponse<SuscripcionInfoDto>> ObtenerMiSuscripcion();
        Task<ApiResponse<object>> ObtenerTodasAsync(int page, int pageSize);
        Task<ApiResponse<SuscripcionDashboardDto>> ObtenerStatsSuscripciones();
        Task<ApiResponse<string>> SuscribirseConTarjeta(int planId, string paymentMethodId, bool autoRenew);
        Task<ApiResponse<string>> CrearCheckoutSuscripcion(int planId);
        Task<ApiResponse<string>> CancelarPlan();
    }
}
