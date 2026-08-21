using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.Carrito;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;

namespace AdLocalAPI.Services
{
    public class CarritoService : ICarritoService
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
                var idUsuario =
                    _jwtContext.GetUserId();

                var carrito =
                    await _repository.ObtenerCarritoActivoAsync(
                        idUsuario
                    );

                if (carrito == null)
                {
                    return ApiResponse<object>.Success(
                        null,
                        "El carrito se encuentra vacío."
                    );
                }

                var detalles =
                    await _repository.ObtenerDetallesAsync(
                        carrito.Id
                    );

                /*
                 * Si existe un carrito activo pero no contiene
                 * detalles activos, lo cerramos.
                 */
                if (detalles.Count == 0)
                {
                    carrito.Subtotal = 0;

                    carrito.Activo = false;

                    carrito.FechaActualizacion =
                        DateTime.UtcNow;

                    await _repository.ActualizarCarritoAsync(
                        carrito
                    );

                    return ApiResponse<object>.Success(
                        null,
                        "El carrito se encuentra vacío."
                    );
                }

                var subtotal =
                    detalles.Sum(
                        x => x.Subtotal
                    );

                /*
                 * Sincronizamos el subtotal almacenado.
                 */
                if (carrito.Subtotal != subtotal)
                {
                    carrito.Subtotal =
                        subtotal;

                    carrito.FechaActualizacion =
                        DateTime.UtcNow;

                    await _repository.ActualizarCarritoAsync(
                        carrito
                    );
                }

                /*
                 * Agrupamos los productos por comercio.
                 */
                var comercios =
                    detalles
                        .GroupBy(x => new
                        {
                            x.IdComercio,
                            x.ComercioUuid,
                            x.ComercioNombre,
                            x.ComercioLogoUrl
                        })
                        .Select(grupo =>
                            new CarritoComercioResponseDto
                            {
                                IdComercio =
                                    grupo.Key.IdComercio,

                                ComercioUuid =
                                    grupo.Key.ComercioUuid,

                                Comercio =
                                    grupo.Key.ComercioNombre,

                                ComercioLogoUrl =
                                    grupo.Key.ComercioLogoUrl,

                                TotalProductos =
                                    grupo.Sum(
                                        x => x.Cantidad
                                    ),

                                Subtotal =
                                    grupo.Sum(
                                        x => x.Subtotal
                                    ),

                                Productos =
                                    grupo.ToList()
                            }
                        )
                        .OrderBy(x => x.Comercio)
                        .ToList();

                var response =
                    new CarritoResponseDto
                    {
                        Uuid =
                            carrito.Uuid,

                        Subtotal =
                            subtotal,

                        TotalProductos =
                            detalles.Sum(
                                x => x.Cantidad
                            ),

                        TotalComercios =
                            comercios.Count,

                        FechaCreacion =
                            carrito.FechaCreacion,

                        Comercios =
                            comercios
                    };

                return ApiResponse<object>.Success(
                    response,
                    "Carrito obtenido correctamente."
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<object>.Error(
                    "401",
                    ex.Message
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error(
                    "500",
                    $"Ocurrió un error al obtener el carrito: {ex.Message}"
                );
            }
        }

        // ============================================================
        // AGREGAR PRODUCTO
        // ============================================================

        public async Task<ApiResponse<object>> AgregarProducto(
            AgregarProductoCarritoDto dto)
        {
            try
            {
                var idUsuario =
                    _jwtContext.GetUserId();

                if (dto == null)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "La información del producto es requerida."
                    );
                }

                if (dto.ProductoUuid == Guid.Empty)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El producto es requerido."
                    );
                }

                if (dto.Cantidad <= 0)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "La cantidad debe ser mayor a 0."
                    );
                }

                // =========================
                // PRODUCTO
                // =========================

                var producto =
                    await _repository.ObtenerProductoPorUuidAsync(
                        dto.ProductoUuid
                    );

                if (producto == null)
                {
                    return ApiResponse<object>.Error(
                        "404",
                        "El producto no existe."
                    );
                }

                if (producto.Eliminado)
                {
                    return ApiResponse<object>.Error(
                        "404",
                        "El producto no se encuentra disponible."
                    );
                }

                if (!producto.Activo)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El producto se encuentra desactivado."
                    );
                }

                if (!producto.Visible)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El producto no se encuentra disponible."
                    );
                }

                if (!producto.Disponible)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El producto está agotado o no está disponible actualmente."
                    );
                }

                if (
                    producto.Tipo !=
                    TipoProductoServicio.Producto
                )
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El elemento seleccionado no corresponde a un producto."
                    );
                }

                if (
                    producto.Modalidad !=
                    ModalidadProductoServicio.Compra
                )
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El producto no permite compra directa."
                    );
                }

                if (!producto.Precio.HasValue)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El producto no tiene un precio definido."
                    );
                }

                if (producto.Precio.Value < 0)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El precio del producto no es válido."
                    );
                }

                // =========================
                // COMERCIO
                // =========================

                var comercio =
                    await _repository.ObtenerComercioAsync(
                        producto.IdComercio
                    );

                if (comercio == null)
                {
                    return ApiResponse<object>.Error(
                        "404",
                        "El comercio no existe."
                    );
                }

                if (!comercio.Activo)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El comercio no se encuentra activo."
                    );
                }

                if (!comercio.Visible)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El comercio no se encuentra disponible."
                    );
                }

                // =========================
                // CONFIGURACIÓN DEL COMERCIO
                // =========================

                /*
                var configuracion =
                    await _repository.ObtenerConfiguracionComercioAsync(
                        producto.IdComercio
                    );

                if (configuracion == null)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El comercio no tiene habilitado el sistema de pedidos."
                    );
                }

                if (!configuracion.AceptaPedidos)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El comercio no acepta pedidos."
                    );
                }

                if (!configuracion.AceptandoPedidosAhora)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El comercio ha pausado temporalmente la recepción de pedidos."
                    );
                }
                */

                // =========================
                // CARRITO
                // =========================

                var carrito =
                    await _repository.ObtenerCarritoActivoAsync(
                        idUsuario
                    );

                /*
                 * YA NO validamos el comercio.
                 *
                 * Un carrito puede contener productos
                 * de múltiples comercios.
                 */

                if (carrito == null)
                {
                    carrito = new Carrito
                    {
                        Uuid =
                            Guid.NewGuid(),

                        IdUsuario =
                            idUsuario,

                        Subtotal =
                            0,

                        Activo =
                            true,

                        FechaCreacion =
                            DateTime.UtcNow
                    };

                    await _repository.CrearCarritoAsync(
                        carrito
                    );
                }

                // =========================
                // DETALLE
                // =========================

                var detalle =
                    await _repository.ObtenerDetalleProductoAsync(
                        carrito.Id,
                        producto.Id
                    );

                var cantidadActual =
                    detalle != null &&
                    detalle.Activo
                        ? detalle.Cantidad
                        : 0;

                var nuevaCantidad =
                    cantidadActual +
                    dto.Cantidad;

                // =========================
                // STOCK
                // =========================

                if (producto.ManejaStock)
                {
                    var stockDisponible =
                        producto.Stock ?? 0;

                    if (stockDisponible <= 0)
                    {
                        return ApiResponse<object>.Error(
                            "400",
                            "El producto se encuentra agotado."
                        );
                    }

                    if (
                        nuevaCantidad >
                        stockDisponible
                    )
                    {
                        return ApiResponse<object>.Error(
                            "400",
                            $"No existe suficiente stock. Disponible: {stockDisponible}."
                        );
                    }
                }

                // =========================
                // CREAR DETALLE
                // =========================

                if (detalle == null)
                {
                    detalle =
                        new CarritoDetalle
                        {
                            Uuid =
                                Guid.NewGuid(),

                            IdCarrito =
                                carrito.Id,

                            IdProductoServicio =
                                producto.Id,

                            Cantidad =
                                dto.Cantidad,

                            PrecioUnitario =
                                producto.Precio.Value,

                            Subtotal =
                                producto.Precio.Value *
                                dto.Cantidad,

                            Observaciones =
                                string.IsNullOrWhiteSpace(
                                    dto.Observaciones
                                )
                                    ? null
                                    : dto.Observaciones.Trim(),

                            Activo =
                                true,

                            FechaCreacion =
                                DateTime.UtcNow
                        };

                    await _repository.CrearDetalleAsync(
                        detalle
                    );
                }
                else
                {
                    /*
                     * Reactivamos si anteriormente
                     * había sido eliminado.
                     */
                    detalle.Activo =
                        true;

                    detalle.Cantidad =
                        nuevaCantidad;

                    detalle.PrecioUnitario =
                        producto.Precio.Value;

                    detalle.Subtotal =
                        producto.Precio.Value *
                        nuevaCantidad;

                    detalle.Observaciones =
                        string.IsNullOrWhiteSpace(
                            dto.Observaciones
                        )
                            ? detalle.Observaciones
                            : dto.Observaciones.Trim();

                    detalle.FechaActualizacion =
                        DateTime.UtcNow;

                    await _repository.ActualizarDetalleAsync(
                        detalle
                    );
                }

                await ActualizarSubtotalCarrito(
                    carrito
                );

                return ApiResponse<object>.Success(
                    new
                    {
                        carritoUuid =
                            carrito.Uuid,

                        detalleUuid =
                            detalle.Uuid,

                        productoUuid =
                            producto.Uuid,

                        comercioUuid =
                            comercio.Uuid,

                        cantidad =
                            detalle.Cantidad,

                        subtotalDetalle =
                            detalle.Subtotal,

                        subtotalCarrito =
                            carrito.Subtotal
                    },
                    "Producto agregado al carrito correctamente."
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<object>.Error(
                    "401",
                    ex.Message
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error(
                    "500",
                    $"Ocurrió un error al agregar el producto al carrito: {ex.Message}"
                );
            }
        }

        // ============================================================
        // ACTUALIZAR CANTIDAD
        // ============================================================

        public async Task<ApiResponse<object>> ActualizarCantidad(
            ActualizarCantidadCarritoDto dto)
        {
            try
            {
                var idUsuario =
                    _jwtContext.GetUserId();

                if (dto == null)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "La información es requerida."
                    );
                }

                if (dto.DetalleUuid == Guid.Empty)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El producto del carrito es requerido."
                    );
                }

                if (dto.Cantidad <= 0)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "La cantidad debe ser mayor a 0."
                    );
                }

                var carrito =
                    await _repository.ObtenerCarritoActivoAsync(
                        idUsuario
                    );

                if (carrito == null)
                {
                    return ApiResponse<object>.Error(
                        "404",
                        "No existe un carrito activo."
                    );
                }

                var detalle =
                    await _repository.ObtenerDetallePorUuidAsync(
                        idUsuario,
                        dto.DetalleUuid
                    );

                if (detalle == null)
                {
                    return ApiResponse<object>.Error(
                        "404",
                        "El producto no se encuentra en el carrito."
                    );
                }

                var producto =
                    await _repository.ObtenerProductoPorIdAsync(
                        detalle.IdProductoServicio
                    );

                if (
                    producto == null ||
                    producto.Eliminado ||
                    !producto.Activo ||
                    !producto.Visible
                )
                {
                    return ApiResponse<object>.Error(
                        "404",
                        "El producto ya no se encuentra disponible."
                    );
                }

                if (!producto.Disponible)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El producto no se encuentra disponible actualmente."
                    );
                }

                /*
                 * Como estamos en carrito,
                 * debe seguir siendo compra directa.
                 */
                if (
                    producto.Tipo !=
                    TipoProductoServicio.Producto ||
                    producto.Modalidad !=
                    ModalidadProductoServicio.Compra
                )
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El producto ya no permite compra directa."
                    );
                }

                if (!producto.Precio.HasValue)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El producto no tiene un precio válido."
                    );
                }

                if (producto.ManejaStock)
                {
                    var stockDisponible =
                        producto.Stock ?? 0;

                    if (
                        dto.Cantidad >
                        stockDisponible
                    )
                    {
                        return ApiResponse<object>.Error(
                            "400",
                            $"No existe suficiente stock. Disponible: {stockDisponible}."
                        );
                    }
                }

                /*
                 * Siempre utilizamos el precio
                 * actual del producto.
                 */
                detalle.Cantidad =
                    dto.Cantidad;

                detalle.PrecioUnitario =
                    producto.Precio.Value;

                detalle.Subtotal =
                    producto.Precio.Value *
                    dto.Cantidad;

                detalle.FechaActualizacion =
                    DateTime.UtcNow;

                await _repository.ActualizarDetalleAsync(
                    detalle
                );

                await ActualizarSubtotalCarrito(
                    carrito
                );

                return ApiResponse<object>.Success(
                    new
                    {
                        detalleUuid =
                            detalle.Uuid,

                        cantidad =
                            detalle.Cantidad,

                        precioUnitario =
                            detalle.PrecioUnitario,

                        subtotalDetalle =
                            detalle.Subtotal,

                        subtotalCarrito =
                            carrito.Subtotal
                    },
                    "Cantidad actualizada correctamente."
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<object>.Error(
                    "401",
                    ex.Message
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error(
                    "500",
                    $"Ocurrió un error al actualizar la cantidad: {ex.Message}"
                );
            }
        }

        // ============================================================
        // ELIMINAR PRODUCTO
        // ============================================================

        public async Task<ApiResponse<object>> EliminarProducto(
            Guid detalleUuid)
        {
            try
            {
                var idUsuario =
                    _jwtContext.GetUserId();

                if (detalleUuid == Guid.Empty)
                {
                    return ApiResponse<object>.Error(
                        "400",
                        "El producto del carrito es requerido."
                    );
                }

                var carrito =
                    await _repository.ObtenerCarritoActivoAsync(
                        idUsuario
                    );

                if (carrito == null)
                {
                    return ApiResponse<object>.Error(
                        "404",
                        "No existe un carrito activo."
                    );
                }

                var detalle =
                    await _repository.ObtenerDetallePorUuidAsync(
                        idUsuario,
                        detalleUuid
                    );

                if (detalle == null)
                {
                    return ApiResponse<object>.Error(
                        "404",
                        "El producto no se encuentra en el carrito."
                    );
                }

                await _repository.DesactivarDetalleAsync(
                    detalle
                );

                var totalDetalles =
                    await _repository.ContarDetallesActivosAsync(
                        carrito.Id
                    );

                /*
                 * Si ya no queda ningún producto
                 * de ningún comercio, cerramos el carrito.
                 */
                if (totalDetalles == 0)
                {
                    carrito.Subtotal =
                        0;

                    carrito.Activo =
                        false;

                    carrito.FechaActualizacion =
                        DateTime.UtcNow;

                    await _repository.ActualizarCarritoAsync(
                        carrito
                    );
                }
                else
                {
                    await ActualizarSubtotalCarrito(
                        carrito
                    );
                }

                return ApiResponse<object>.Success(
                    new
                    {
                        subtotal =
                            carrito.Subtotal,

                        carritoVacio =
                            totalDetalles == 0
                    },
                    "Producto eliminado del carrito correctamente."
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<object>.Error(
                    "401",
                    ex.Message
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error(
                    "500",
                    $"Ocurrió un error al eliminar el producto del carrito: {ex.Message}"
                );
            }
        }

        // ============================================================
        // VACIAR CARRITO
        // ============================================================

        public async Task<ApiResponse<object>> VaciarCarrito()
        {
            try
            {
                var idUsuario =
                    _jwtContext.GetUserId();

                var carrito =
                    await _repository.ObtenerCarritoActivoAsync(
                        idUsuario
                    );

                if (carrito == null)
                {
                    return ApiResponse<object>.Success(
                        null,
                        "El carrito ya se encuentra vacío."
                    );
                }

                await _repository.VaciarCarritoAsync(
                    carrito
                );

                return ApiResponse<object>.Success(
                    null,
                    "Carrito vaciado correctamente."
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResponse<object>.Error(
                    "401",
                    ex.Message
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error(
                    "500",
                    $"Ocurrió un error al vaciar el carrito: {ex.Message}"
                );
            }
        }

        // ============================================================
        // RECALCULAR SUBTOTAL
        // ============================================================

        private async Task ActualizarSubtotalCarrito(
            Carrito carrito)
        {
            carrito.Subtotal =
                await _repository.CalcularSubtotalAsync(
                    carrito.Id
                );

            carrito.FechaActualizacion =
                DateTime.UtcNow;

            await _repository.ActualizarCarritoAsync(
                carrito
            );
        }
    }
}