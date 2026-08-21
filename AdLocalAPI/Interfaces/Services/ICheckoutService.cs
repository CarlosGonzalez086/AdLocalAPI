using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services.Interfaces
{
    public interface ICheckoutService
    {
        Task<ApiResponse<CheckoutResponseDto>>
            ObtenerCheckout();

        Task<ApiResponse<ConfirmarCheckoutResponseDto>>
            Confirmar(
                ConfirmarCheckoutDto dto
            );
    }
}