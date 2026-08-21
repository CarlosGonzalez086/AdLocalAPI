using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IComprobantePagoService
    {
        Task<ApiResponse<ComprobanteTransferenciaResponseDto>> SubirAsync(
            Guid pedidoUuid,
            SubirComprobanteTransferenciaDto comprobante
        );
    }
}
