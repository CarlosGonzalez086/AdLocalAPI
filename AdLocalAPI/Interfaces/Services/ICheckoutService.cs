using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services.Interfaces
{
    public interface ICheckoutService
    {
        Task<ApiResponse<CheckoutResponseDto>>
            ObtenerCheckout(CancellationToken cancellationToken = default);

        Task<ApiResponse<ConfirmarCheckoutResponseDto>>
            Confirmar(
                ConfirmarCheckoutDto dto,
                CancellationToken cancellationToken = default
            );
    }
}