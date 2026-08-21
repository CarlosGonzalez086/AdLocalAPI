using AdLocalAPI.DTOs.Carrito;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface ICarritoRepository
    {
        Task<Carrito?> ObtenerCarritoActivoAsync(long idUsuario);

        Task<CarritoDetalle?> ObtenerDetallePorUuidAsync(
            long idUsuario,
            Guid detalleUuid
        );

        Task<CarritoDetalle?> ObtenerDetalleProductoAsync(
            long idCarrito,
            long idProductoServicio
        );

        Task<List<CarritoDetalleResponseDto>> ObtenerDetallesAsync(
            long idCarrito
        );

        Task<ProductosServicios?> ObtenerProductoPorUuidAsync(
            Guid productoUuid
        );

        Task<ProductosServicios?> ObtenerProductoPorIdAsync(
            long idProductoServicio
        );

        Task<Comercio?> ObtenerComercioAsync(long idComercio);

        Task<ConfiguracionComercioPedido?>
            ObtenerConfiguracionComercioAsync(long idComercio);

        Task<Carrito> CrearCarritoAsync(Carrito carrito);

        Task<CarritoDetalle> CrearDetalleAsync(
            CarritoDetalle detalle
        );

        Task ActualizarCarritoAsync(Carrito carrito);

        Task ActualizarDetalleAsync(CarritoDetalle detalle);

        Task DesactivarDetalleAsync(CarritoDetalle detalle);

        Task VaciarCarritoAsync(Carrito carrito);

        Task<decimal> CalcularSubtotalAsync(long idCarrito);

        Task<int> ContarDetallesActivosAsync(long idCarrito);
    }
}