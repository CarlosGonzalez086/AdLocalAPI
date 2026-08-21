using AdLocalAPI.Constants;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using System.Globalization;

namespace AdLocalAPI.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly IPedidoRepository _repository;
        private readonly JwtContext _jwtContext;
        private readonly INotificacionService _notificaciones;

        public CheckoutService(
            IPedidoRepository repository,
            JwtContext jwtContext,
            INotificacionService notificaciones)
        {
            _repository = repository;
            _jwtContext = jwtContext;
            _notificaciones = notificaciones;
        }

        // ==========================================
        // OBTENER CHECKOUT
        // ==========================================

        public async Task<ApiResponse<CheckoutResponseDto>>
            ObtenerCheckout()
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
                    return ApiResponse<CheckoutResponseDto>.Error(
                        "404",
                        "No tienes un carrito activo."
                    );
                }

                var items =
                    await _repository.ObtenerProductosCarritoAsync(
                        carrito.Id
                    );

                if (items.Count == 0)
                {
                    return ApiResponse<CheckoutResponseDto>.Error(
                        "400",
                        "Tu carrito está vacío."
                    );
                }

                var response =
                    new CheckoutResponseDto();

                var grupos =
                    items.GroupBy(x =>
                        x.Producto.IdComercio
                    );

                foreach (var grupo in grupos)
                {
                    var primero =
                        grupo.First();

                    var comercio =
                        primero.Comercio;

                    var configuracion =
                        await _repository
                            .ObtenerConfiguracionPagoAsync(
                                comercio.Id
                            );

                    if (configuracion == null)
                    {
                        return ApiResponse<CheckoutResponseDto>.Error(
                            "400",
                            $"El comercio {comercio.Nombre} todavía no tiene configurados sus métodos de pago."
                        );
                    }

                    CuentaBancariaComercio? cuenta =
                        null;

                    if (configuracion.AceptaTransferencia)
                    {
                        cuenta =
                            await _repository
                                .ObtenerCuentaPrincipalAsync(
                                    comercio.Id
                                );
                    }

                    var productos =
                        grupo.Select(x =>
                        {
                            var precio =
                                x.Producto.Precio ?? 0;

                            return new CheckoutProductoDto
                            {
                                ProductoUuid =
                                    x.Producto.Uuid,

                                Nombre =
                                    x.Producto.Nombre,

                                LogoUrl =
                                    x.Producto.LogoUrl,

                                Cantidad =
                                    x.Cantidad,

                                PrecioUnitario =
                                    precio,

                                Subtotal =
                                    precio * x.Cantidad,

                                PermiteDomicilio =
                                    x.Producto.PermiteDomicilio,

                                PermiteRecoger =
                                    x.Producto.PermiteRecoger
                            };
                        })
                        .ToList();

                    var subtotal =
                        productos.Sum(x =>
                            x.Subtotal
                        );

                    response.Comercios.Add(
                        new CheckoutComercioResponseDto
                        {
                            ComercioUuid =
                                comercio.Uuid,

                            Comercio =
                                comercio.Nombre,

                            LogoUrl =
                                comercio.LogoUrl,

                            Subtotal =
                                subtotal,

                            AceptaEfectivo =
                                configuracion.AceptaEfectivo,

                            AceptaTransferencia =
                                configuracion.AceptaTransferencia &&
                                cuenta != null,

                            /*
                             * Para permitir domicilio TODOS
                             * los productos deben permitirlo.
                             */
                            PermiteDomicilio =
                                productos.All(x =>
                                    x.PermiteDomicilio
                                ),

                            PermiteRecoger =
                                productos.All(x =>
                                    x.PermiteRecoger
                                ),

                            InstruccionesTransferencia =
                                configuracion.InstruccionesTransferencia,

                            CuentaTransferencia =
                                cuenta == null
                                    ? null
                                    : new CuentaTransferenciaCheckoutDto
                                    {
                                        Banco =
                                            cuenta.Banco,

                                        Beneficiario =
                                            cuenta.Beneficiario,

                                        NumeroCuenta =
                                            cuenta.NumeroCuenta,

                                        Clabe =
                                            cuenta.Clabe,

                                        NumeroTarjeta =
                                            cuenta.NumeroTarjeta
                                    },

                            Productos =
                                productos
                        }
                    );

                    response.TotalGeneral +=
                        subtotal;
                }

                return ApiResponse<CheckoutResponseDto>.Success(
                    response,
                    "Checkout obtenido correctamente."
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<CheckoutResponseDto>.Error(
                    "500",
                    ex.Message
                );
            }
        }

        // ==========================================
        // CONFIRMAR CHECKOUT
        // ==========================================

        public async Task<ApiResponse<ConfirmarCheckoutResponseDto>>
            Confirmar(
                ConfirmarCheckoutDto dto)
        {
            try
            {
                var idUsuario =
                    _jwtContext.GetUserId();

                var usuario =
                    await _repository.ObtenerUsuarioAsync(
                        idUsuario
                    );

                if (usuario == null)
                {
                    return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                        "404",
                        "Usuario no encontrado."
                    );
                }

                var carrito =
                    await _repository.ObtenerCarritoActivoAsync(
                        idUsuario
                    );

                if (carrito == null)
                {
                    return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                        "404",
                        "No tienes un carrito activo."
                    );
                }

                var items =
                    await _repository.ObtenerProductosCarritoAsync(
                        carrito.Id
                    );

                if (items.Count == 0)
                {
                    return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                        "400",
                        "Tu carrito está vacío."
                    );
                }

                var grupos =
                    items
                        .GroupBy(x =>
                            x.Producto.IdComercio
                        )
                        .ToList();

                if (dto.Comercios == null ||
                    dto.Comercios.Count != grupos.Count)
                {
                    return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                        "400",
                        "Debes configurar el pago y entrega de todos los comercios del carrito."
                    );
                }

                // ==========================================
                // COMISIÓN
                // ==========================================

                var porcentajeComision =
                    await ObtenerDecimalConfiguracion(
                        ConfiguracionKeys
                            .MarketplaceCommissionPercentage
                    );

                var comisionFija =
                    await ObtenerDecimalConfiguracion(
                        ConfiguracionKeys
                            .MarketplaceCommissionFixed
                    );

                var comisionActiva =
                    await ObtenerBooleanConfiguracion(
                        ConfiguracionKeys
                            .MarketplaceCommissionEnabled
                    );

                if (!comisionActiva)
                {
                    porcentajeComision = 0;
                    comisionFija = 0;
                }

                var pedidos =
                    new List<Pedido>();

                var productosActualizar =
                    new List<ProductosServicios>();

                var response =
                    new ConfirmarCheckoutResponseDto();

                // ==========================================
                // PROCESAR CADA COMERCIO
                // ==========================================

                foreach (var grupo in grupos)
                {
                    var comercio =
                        grupo.First().Comercio;

                    var configuracionCliente =
                        dto.Comercios.FirstOrDefault(x =>
                            x.ComercioUuid == comercio.Uuid
                        );

                    if (configuracionCliente == null)
                    {
                        return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                            "400",
                            $"Falta configurar el pedido de {comercio.Nombre}."
                        );
                    }

                    var configuracionPago =
                        await _repository.ObtenerConfiguracionPagoAsync(
                            comercio.Id
                        );

                    if (configuracionPago == null)
                    {
                        return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                            "400",
                            $"El comercio {comercio.Nombre} no tiene métodos de pago configurados."
                        );
                    }

                    // ==========================================
                    // VALIDAR PAGO
                    // ==========================================

                    CuentaBancariaComercio? cuenta =
                        null;

                    if (
                        configuracionCliente.MetodoPago ==
                        MetodoPagoPedido.Efectivo
                    )
                    {
                        if (!configuracionPago.AceptaEfectivo)
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"{comercio.Nombre} no acepta pagos en efectivo."
                            );
                        }
                    }
                    else if (
                        configuracionCliente.MetodoPago ==
                        MetodoPagoPedido.Transferencia
                    )
                    {
                        if (!configuracionPago.AceptaTransferencia)
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"{comercio.Nombre} no acepta transferencias."
                            );
                        }

                        cuenta =
                            await _repository.ObtenerCuentaPrincipalAsync(
                                comercio.Id
                            );

                        if (cuenta == null)
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"{comercio.Nombre} no tiene una cuenta bancaria disponible."
                            );
                        }
                    }
                    else
                    {
                        return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                            "400",
                            "Método de pago inválido."
                        );
                    }

                    // ==========================================
                    // VALIDAR ENTREGA
                    // ==========================================

                    DireccionCheckoutDto? direccion =
                        null;

                    if (
                        configuracionCliente.TipoEntrega ==
                        TipoEntregaPedido.Domicilio
                    )
                    {
                        if (
                            grupo.Any(x =>
                                !x.Producto.PermiteDomicilio
                            )
                        )
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"Uno o más productos de {comercio.Nombre} no permiten entrega a domicilio."
                            );
                        }

                        if (!configuracionCliente.DireccionUuid.HasValue)
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                "Debes seleccionar una dirección de entrega."
                            );
                        }

                        direccion =
                            await _repository.ObtenerDireccionAsync(
                                idUsuario,
                                configuracionCliente
                                    .DireccionUuid
                                    .Value
                            );

                        if (direccion == null)
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                "La dirección seleccionada no es válida."
                            );
                        }
                    }
                    else if (
                        configuracionCliente.TipoEntrega ==
                        TipoEntregaPedido.Recoger
                    )
                    {
                        if (
                            grupo.Any(x =>
                                !x.Producto.PermiteRecoger
                            )
                        )
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"Uno o más productos de {comercio.Nombre} no permiten recoger en el establecimiento."
                            );
                        }
                    }
                    else
                    {
                        return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                            "400",
                            "Tipo de entrega inválido."
                        );
                    }

                    // ==========================================
                    // VALIDAR PRODUCTOS Y STOCK
                    // ==========================================

                    decimal subtotal =
                        0;

                    var detalles =
                        new List<PedidoDetalle>();

                    foreach (var item in grupo)
                    {
                        var producto =
                            item.Producto;

                        if (
                            !producto.Activo ||
                            producto.Eliminado ||
                            !producto.Visible ||
                            !producto.Disponible
                        )
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"{producto.Nombre} ya no está disponible."
                            );
                        }

                        /*
                         * Solamente productos/servicios de
                         * modalidad Compra van al carrito.
                         */
                        if (
                            producto.Modalidad !=
                            ModalidadProductoServicio.Compra
                        )
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"{producto.Nombre} no puede procesarse como una compra."
                            );
                        }

                        if (!producto.Precio.HasValue)
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"{producto.Nombre} no tiene un precio válido."
                            );
                        }

                        if (item.Cantidad <= 0)
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"La cantidad de {producto.Nombre} no es válida."
                            );
                        }

                        if (producto.ManejaStock)
                        {
                            var stock =
                                producto.Stock ?? 0;

                            if (stock < item.Cantidad)
                            {
                                return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                    "400",
                                    $"No hay suficiente stock de {producto.Nombre}."
                                );
                            }

                            producto.Stock =
                                stock - item.Cantidad;

                            producto.FechaActualizacion =
                                DateTime.UtcNow;

                            productosActualizar.Add(
                                producto
                            );
                        }

                        var precio =
                            producto.Precio.Value;

                        var subtotalDetalle =
                            precio * item.Cantidad;

                        subtotal +=
                            subtotalDetalle;

                        detalles.Add(
                            new PedidoDetalle
                            {
                                Uuid =
                                    Guid.NewGuid(),

                                IdProductoServicio =
                                    producto.Id,

                                ProductoUuid =
                                    producto.Uuid,

                                Nombre =
                                    producto.Nombre,

                                Descripcion =
                                    producto.Descripcion,

                                LogoUrl =
                                    producto.LogoUrl,

                                CodigoInterno =
                                    producto.CodigoInterno,

                                Cantidad =
                                    item.Cantidad,

                                PrecioUnitario =
                                    precio,

                                Subtotal =
                                    subtotalDetalle,

                                Observaciones =
                                    item.Observaciones,

                                FechaCreacion =
                                    DateTime.UtcNow
                            }
                        );
                    }

                    // ==========================================
                    // CALCULAR COMISIÓN
                    // ==========================================

                    var montoComisionPorcentaje =
                        subtotal *
                        (porcentajeComision / 100m);

                    var montoComision =
                        Math.Round(
                            montoComisionPorcentaje +
                            comisionFija,
                            2
                        );

                    /*
                     * No permitimos que la comisión
                     * sea superior al subtotal.
                     */
                    if (montoComision > subtotal)
                    {
                        montoComision =
                            subtotal;
                    }

                    var montoComercio =
                        subtotal -
                        montoComision;

                    // ==========================================
                    // CREAR PEDIDO
                    // ==========================================

                    var pedido =
                        new Pedido
                        {
                            Uuid =
                                Guid.NewGuid(),

                            NumeroPedido =
                                GenerarNumeroPedido(),

                            IdUsuario =
                                idUsuario,

                            IdComercio =
                                comercio.Id,

                            IdDireccionUsuario =
                                direccion?.Id,

                            Estado =
                                EstadoPedido
                                    .PendienteAprobacion,

                            EstadoPago =
                                configuracionCliente.MetodoPago ==
                                MetodoPagoPedido.Transferencia
                                    ? EstadoPagoPedido.PendienteComprobante
                                    : EstadoPagoPedido.Pendiente,

                            MetodoPago =
                                configuracionCliente.MetodoPago,

                            TipoEntrega =
                                configuracionCliente.TipoEntrega,

                            Subtotal =
                                subtotal,

                            Total =
                                subtotal,

                            PorcentajeComision =
                                porcentajeComision,

                            ComisionFija =
                                comisionFija,

                            MontoComision =
                                montoComision,

                            MontoComercio =
                                montoComercio,

                            ComercioNombre =
                                comercio.Nombre,

                            ComercioLogoUrl =
                                comercio.LogoUrl,

                            ClienteNombre =
                                usuario.Nombre,

                            ClienteEmail =
                                usuario.Email,

                            ObservacionesCliente =
                                string.IsNullOrWhiteSpace(
                                    configuracionCliente.Observaciones
                                )
                                    ? null
                                    : configuracionCliente
                                        .Observaciones
                                        .Trim(),

                            FechaCreacion =
                                DateTime.UtcNow,

                            Detalles =
                                detalles
                        };

                    // ==========================================
                    // SNAPSHOT DIRECCIÓN
                    // ==========================================

                    if (direccion != null)
                    {
                        pedido.DireccionAlias =
                            direccion.Alias;

                        pedido.DireccionCalle =
                            direccion.Calle;

                        pedido.DireccionNumeroExterior =
                            direccion.NumeroExterior;

                        pedido.DireccionNumeroInterior =
                            direccion.NumeroInterior;

                        pedido.DireccionColonia =
                            direccion.Colonia;

                        pedido.DireccionCodigoPostal =
                            direccion.CodigoPostal;

                        pedido.DireccionEstado =
                            direccion.Estado;

                        pedido.DireccionMunicipio =
                            direccion.Municipio;

                        pedido.DireccionLatitud =
                            direccion.Latitud;

                        pedido.DireccionLongitud =
                            direccion.Longitud;

                        pedido.DireccionReferencias =
                            direccion.Referencias;

                        pedido.TelefonoEntrega =
                            direccion.Telefono;
                    }

                    // ==========================================
                    // SNAPSHOT CUENTA BANCARIA
                    // ==========================================

                    if (cuenta != null)
                    {
                        pedido.Banco =
                            cuenta.Banco;

                        pedido.Beneficiario =
                            cuenta.Beneficiario;

                        pedido.NumeroCuenta =
                            cuenta.NumeroCuenta;

                        pedido.Clabe =
                            cuenta.Clabe;

                        pedido.NumeroTarjeta =
                            cuenta.NumeroTarjeta;

                        pedido.InstruccionesTransferencia =
                            configuracionPago
                                .InstruccionesTransferencia;
                    }

                    // ==========================================
                    // HISTORIAL INICIAL
                    // ==========================================

                    pedido.HistorialEstados.Add(
                        new PedidoHistorialEstado
                        {
                            Uuid =
                                Guid.NewGuid(),

                            EstadoAnterior =
                                null,

                            EstadoNuevo =
                                EstadoPedido
                                    .PendienteAprobacion,

                            IdUsuarioCambio =
                                idUsuario,

                            Comentario =
                                "Pedido creado por el cliente.",

                            FechaCreacion =
                                DateTime.UtcNow
                        }
                    );

                    pedidos.Add(
                        pedido
                    );

                    response.Pedidos.Add(
                        new PedidoCheckoutResponseDto
                        {
                            Uuid =
                                pedido.Uuid,

                            NumeroPedido =
                                pedido.NumeroPedido,

                            ComercioUuid =
                                comercio.Uuid,

                            Comercio =
                                comercio.Nombre,

                            Total =
                                subtotal,

                            Estado =
                                (int)pedido.Estado,

                            EstadoPago =
                                (int)pedido.EstadoPago,

                            MetodoPago =
                                (int)pedido.MetodoPago,

                            TipoEntrega =
                                (int)pedido.TipoEntrega,

                            RequiereComprobante =
                                pedido.MetodoPago == MetodoPagoPedido.Transferencia
                        }
                    );

                    response.TotalGeneral +=
                        subtotal;
                }

                // ==========================================
                // GUARDAR TODO EN UNA TRANSACCIÓN
                // ==========================================

                await _repository.GuardarCheckoutAsync(
                    pedidos,
                    productosActualizar,
                    carrito
                );

                foreach (var pedidoCreado in pedidos)
                {
                    await _notificaciones.NotificarComercioAsync(
                        pedidoCreado,
                        TipoNotificacionPedido.PedidoCreado,
                        "Nuevo pedido",
                        $"{pedidoCreado.ClienteNombre} creó el pedido {pedidoCreado.NumeroPedido} por {pedidoCreado.Total:C}."
                    );
                }

                response.TotalPedidos =
                    pedidos.Count;

                return ApiResponse<ConfirmarCheckoutResponseDto>.Success(
                    response,
                    pedidos.Count == 1
                        ? "Pedido creado correctamente."
                        : $"{pedidos.Count} pedidos creados correctamente."
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                    "500",
                    ex.Message
                );
            }
        }

        // ==========================================
        // CONFIGURACIÓN DECIMAL
        // ==========================================

        private async Task<decimal>
            ObtenerDecimalConfiguracion(
                string key)
        {
            var config =
                await _repository.ObtenerConfiguracionAsync(
                    key
                );

            if (config == null)
            {
                return 0;
            }

            return decimal.TryParse(
                config.Val,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var value
            )
                ? value
                : 0;
        }

        // ==========================================
        // CONFIGURACIÓN BOOLEAN
        // ==========================================

        private async Task<bool>
            ObtenerBooleanConfiguracion(
                string key)
        {
            var config =
                await _repository.ObtenerConfiguracionAsync(
                    key
                );

            if (config == null)
            {
                return false;
            }

            return bool.TryParse(
                config.Val,
                out var value
            ) && value;
        }

        // ==========================================
        // NÚMERO DE PEDIDO
        // ==========================================

        private static string GenerarNumeroPedido()
        {
            return $"ADL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }
    }
}
