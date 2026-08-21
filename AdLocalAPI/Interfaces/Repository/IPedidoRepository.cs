using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Models;

using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.UsuarioCliente;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IPedidoRepository
    {
        Task<Carrito?> ObtenerCarritoActivoAsync(
            long idUsuario
        );

        Task<List<CheckoutCarritoItemDto>>
            ObtenerProductosCarritoAsync(
                long idCarrito
            );

        Task<Comercio?> ObtenerComercioPorUuidAsync(
            Guid uuid
        );

        Task<ConfiguracionPagoComercio?>
            ObtenerConfiguracionPagoAsync(
                long idComercio
            );

        Task<CuentaBancariaComercio?>
            ObtenerCuentaPrincipalAsync(
                long idComercio
            );

        Task<DireccionCheckoutDto?>
            ObtenerDireccionAsync(
                long idUsuario,
                Guid direccionUuid
            );

        Task<Usuario?> ObtenerUsuarioAsync(
            long idUsuario
        );

        Task<ConfiguracionSistema?>
            ObtenerConfiguracionAsync(
                string key
            );

        Task GuardarCheckoutAsync(
            List<Pedido> pedidos,
            List<ProductosServicios> productosActualizar,
            Carrito carrito
        );

        Task<Pedido?> ObtenerPedidoClienteAsync(
            Guid pedidoUuid,
            long idUsuario
        );

        Task GuardarComprobanteAsync(
            Pedido pedido,
            ComprobantePago comprobante
        );
        Task<PagedResponse<PedidoClienteListadoDto>> ObtenerPedidosClienteAsync(
            long idUsuario,
            int page,
            int pageSize,
            EstadoPagoPedido? estadoPago
        );

        Task<PedidoClienteDetalleDto?> ObtenerDetallePedidoClienteAsync(
            Guid pedidoUuid,
            long idUsuario
        );
    }
}
