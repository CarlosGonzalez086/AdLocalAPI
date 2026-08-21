using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.Carrito;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services.Interfaces
{
    public interface ICarritoService
    {
        Task<ApiResponse<object>> ObtenerCarrito();

        Task<ApiResponse<object>> AgregarProducto(
            AgregarProductoCarritoDto dto
        );

        Task<ApiResponse<object>> ActualizarCantidad(
            ActualizarCantidadCarritoDto dto
        );

        Task<ApiResponse<object>> EliminarProducto(
            Guid detalleUuid
        );

        Task<ApiResponse<object>> VaciarCarrito();
    }
}