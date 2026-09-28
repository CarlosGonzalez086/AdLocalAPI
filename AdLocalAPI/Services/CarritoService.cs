using System;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.Carrito;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;

namespace AdLocalAPI.Services
{
    public partial class CarritoService : ICarritoService
    {
        private readonly ICarritoRepository _repository;
        private readonly JwtContext _jwtContext;

        public CarritoService(
            ICarritoRepository repository,
            JwtContext jwtContext)
        {
            _repository = repository;
            _jwtContext = jwtContext;
        }

        // ============================================================
        // OBTENER CARRITO
        // ============================================================

        public async Task<ApiResponse<object>> ObtenerCarrito()
        {
            try
            {
                var idUsuario = _jwtContext.GetUserId();
                var carrito = await _repository.ObtenerCarritoActivoAsync(idUsuario);

                if (carrito == null)
                {
                    return ApiResponse<object>.Success(null, "El carrito se encuentra vacío.");
                }

                var detalles = await _repository.ObtenerDetallesAsync(carrito.Id);

                if (detalles.Count == 0)
                {
                    carrito.Subtotal = 0;
                    carrito.Activo = false;
                    carrito.FechaActualizacion = DateTime.UtcNow;

                    await _repository.ActualizarCarritoAsync(carrito);

                    return ApiResponse<object>.Success(null, "El carrito se encuentra vacío.");
                }

                var subtotal = detalles.Sum(x => x.Subtotal);

                if (carrito.Subtotal != subtotal)
                {
                    carrito.Subtotal = subtotal;
                    carrito.FechaActualizacion = DateTime.UtcNow;

                    await _repository.ActualizarCarritoAsync(carrito);
                }

                var comercios = detalles
                    .GroupBy(x => new
                    {
                        x.IdComercio,
                        x.ComercioUuid,
                        x.ComercioNombre,
                        x.ComercioLogoUrl
                    })
                    .Select(grupo => new CarritoComercioResponseDto
                    {
                        IdComercio = grupo.Key.IdComercio,
                        ComercioUuid = grupo.Key.ComercioUuid,
                        Comercio = grupo.Key.ComercioNombre,
                        ComercioLogoUrl = grupo.Key.ComercioLogoUrl,
                        TotalProductos = grupo.Sum(x => x.Cantidad),
                        Subtotal = grupo.Sum(x => x.Subtotal),
                        Productos = grupo.ToList()
                    })
                    .OrderBy(x => x.Comercio)
                    .ToList();

                var response = new CarritoResponseDto
                {
                    Uuid = carrito.Uuid,
                    Subtotal = subtotal,
                    TotalProductos = detalles.Sum(x => x.Cantidad),
                    TotalComercios = comercios.Count,
                    FechaCreacion = carrito.FechaCreacion,
                    Comercios = comercios
                };

                return ApiResponse<object>.Success(response, "Carrito obtenido correctamente.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<object>.Error("401", ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", $"Ocurrió un error al obtener el carrito: {ex.Message}");
            }
        }

        // ============================================================
        // VACIAR CARRITO
        // ============================================================

        public async Task<ApiResponse<object>> VaciarCarrito()
        {
            try
            {
                var idUsuario = _jwtContext.GetUserId();
                var carrito = await _repository.ObtenerCarritoActivoAsync(idUsuario);

                if (carrito == null)
                {
                    return ApiResponse<object>.Success(null, "El carrito ya se encuentra vacío.");
                }

                await _repository.VaciarCarritoAsync(carrito);

                return ApiResponse<object>.Success(null, "Carrito vaciado correctamente.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<object>.Error("401", ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", $"Ocurrió un error al vaciar el carrito: {ex.Message}");
            }
        }

        // ============================================================
        // RECALCULAR SUBTOTAL
        // ============================================================

        private async Task ActualizarSubtotalCarrito(Carrito carrito)
        {
            carrito.Subtotal = await _repository.CalcularSubtotalAsync(carrito.Id);
            carrito.FechaActualizacion = DateTime.UtcNow;

            await _repository.ActualizarCarritoAsync(carrito);
        }
    }
}