using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.Constants;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.Extensions.Logging;

namespace AdLocalAPI.Services
{
    public partial class CheckoutService
    {
        // ==========================================
        // CONFIRMAR CHECKOUT
        // ==========================================

        public async Task<ApiResponse<ConfirmarCheckoutResponseDto>> Confirmar(ConfirmarCheckoutDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var idUsuario = _jwtContext.GetUserId();

                var usuario = await _repository.ObtenerUsuarioAsync(idUsuario);
                if (usuario == null)
                {
                    return ApiResponse<ConfirmarCheckoutResponseDto>.Error("404", "Usuario no encontrado.");
                }

                var carrito = await _repository.ObtenerCarritoActivoAsync(idUsuario);
                if (carrito == null)
                {
                    return ApiResponse<ConfirmarCheckoutResponseDto>.Error("404", "No tienes un carrito activo.");
                }

                var items = await _repository.ObtenerProductosCarritoAsync(carrito.Id);
                if (items.Count == 0)
                {
                    return ApiResponse<ConfirmarCheckoutResponseDto>.Error("400", "Tu carrito está vacío.");
                }

                // ==========================================
                // CONTROL DE IDEMPOTENCIA
                // ==========================================
                var idempotencyKey = !string.IsNullOrWhiteSpace(dto.IdempotencyKey)
                    ? dto.IdempotencyKey.Trim()
                    : $"cart_{carrito.Id}";

                var idempotenciaExistente = await _repository.ObtenerIdempotenciaAsync(idUsuario, idempotencyKey);
                if (idempotenciaExistente != null)
                {
                    if (idempotenciaExistente.Status == "completed" && !string.IsNullOrEmpty(idempotenciaExistente.ResponseJson))
                    {
                        try
                        {
                            var cached = System.Text.Json.JsonSerializer.Deserialize<ConfirmarCheckoutResponseDto>(
                                idempotenciaExistente.ResponseJson
                            );

                            if (cached != null)
                            {
                                return ApiResponse<ConfirmarCheckoutResponseDto>.Success(
                                    cached,
                                    "Pedido previamente confirmado (idempotente)."
                                );
                            }
                        }
                        catch
                        {
                            // Ignorar error de deserialización
                        }
                    }

                    if (idempotenciaExistente.Status == "processing")
                    {
                        return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                            "409",
                            "Tu pedido ya se encuentra en procesamiento. Por favor espera unos momentos antes de reintentar."
                        );
                    }
                }

                var idempotenciaRegistro = new CheckoutIdempotencia
                {
                    IdUsuario = idUsuario,
                    IdempotencyKey = idempotencyKey,
                    Status = "processing",
                    FechaCreacion = DateTime.UtcNow
                };

                try
                {
                    await _repository.RegistrarIdempotenciaInicioAsync(idempotenciaRegistro);
                }
                catch
                {
                    return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                        "409",
                        "Tu pedido ya se encuentra en procesamiento."
                    );
                }

                var grupos = items.GroupBy(x => x.Producto.IdComercio).ToList();

                if (dto.Comercios == null || dto.Comercios.Count != grupos.Count)
                {
                    return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                        "400",
                        "Debes configurar el pago y entrega de todos los comercios del carrito."
                    );
                }

                // ==========================================
                // COMISIÓN
                // ==========================================
                var porcentajeComision = await ObtenerDecimalConfiguracion(ConfiguracionKeys.MarketplaceCommissionPercentage);
                var comisionFija = await ObtenerDecimalConfiguracion(ConfiguracionKeys.MarketplaceCommissionFixed);
                var comisionActiva = await ObtenerBooleanConfiguracion(ConfiguracionKeys.MarketplaceCommissionEnabled);

                if (!comisionActiva)
                {
                    porcentajeComision = 0;
                    comisionFija = 0;
                }

                var pedidos = new List<Pedido>();
                var productosActualizarStock = new List<(long Id, int Cantidad, string Nombre)>();
                var response = new ConfirmarCheckoutResponseDto();

                // ==========================================
                // PROCESAR CADA COMERCIO
                // ==========================================
                foreach (var grupo in grupos)
                {
                    var comercio = grupo.First().Comercio;
                    var configuracionCliente = dto.Comercios.FirstOrDefault(x => x.ComercioUuid == comercio.Uuid);

                    if (configuracionCliente == null)
                    {
                        return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                            "400",
                            $"Falta configurar el pedido de {comercio.Nombre}."
                        );
                    }

                    var configuracionPago = await _repository.ObtenerConfiguracionPagoAsync(comercio.Id);
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
                    CuentaBancariaComercio? cuenta = null;

                    if (configuracionCliente.MetodoPago == MetodoPagoPedido.Efectivo)
                    {
                        if (!configuracionPago.AceptaEfectivo)
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"{comercio.Nombre} no acepta pagos en efectivo."
                            );
                        }
                    }
                    else if (configuracionCliente.MetodoPago == MetodoPagoPedido.Transferencia)
                    {
                        if (!configuracionPago.AceptaTransferencia)
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"{comercio.Nombre} no acepta transferencias."
                            );
                        }

                        cuenta = await _repository.ObtenerCuentaPrincipalAsync(comercio.Id);
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
                        return ApiResponse<ConfirmarCheckoutResponseDto>.Error("400", "Método de pago inválido.");
                    }

                    // ==========================================
                    // VALIDAR ENTREGA
                    // ==========================================
                    DireccionCheckoutDto? direccion = null;

                    if (configuracionCliente.TipoEntrega == TipoEntregaPedido.Domicilio)
                    {
                        if (grupo.Any(x => !x.Producto.PermiteDomicilio))
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

                        direccion = await _repository.ObtenerDireccionAsync(
                            idUsuario,
                            configuracionCliente.DireccionUuid.Value
                        );

                        if (direccion == null)
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                "La dirección seleccionada no es válida."
                            );
                        }
                    }
                    else if (configuracionCliente.TipoEntrega == TipoEntregaPedido.Recoger)
                    {
                        if (grupo.Any(x => !x.Producto.PermiteRecoger))
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"Uno o más productos de {comercio.Nombre} no permiten recoger en el establecimiento."
                            );
                        }
                    }
                    else
                    {
                        return ApiResponse<ConfirmarCheckoutResponseDto>.Error("400", "Tipo de entrega inválido.");
                    }

                    // ==========================================
                    // VALIDAR PRODUCTOS Y STOCK
                    // ==========================================
                    decimal subtotal = 0;
                    var detalles = new List<PedidoDetalle>();

                    foreach (var item in grupo)
                    {
                        var producto = item.Producto;

                        if (!producto.Activo || producto.Eliminado || !producto.Visible || !producto.Disponible)
                        {
                            return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                "400",
                                $"{producto.Nombre} ya no está disponible."
                            );
                        }

                        if (producto.Modalidad != ModalidadProductoServicio.Compra)
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
                            var stock = producto.Stock ?? 0;
                            if (stock < item.Cantidad)
                            {
                                return ApiResponse<ConfirmarCheckoutResponseDto>.Error(
                                    "400",
                                    $"No hay suficiente stock de {producto.Nombre}."
                                );
                            }

                            productosActualizarStock.Add((producto.Id, item.Cantidad, producto.Nombre));
                        }

                        var precio = producto.Precio.Value;
                        var subtotalDetalle = precio * item.Cantidad;
                        subtotal += subtotalDetalle;

                        detalles.Add(new PedidoDetalle
                        {
                            Uuid = Guid.NewGuid(),
                            IdProductoServicio = producto.Id,
                            ProductoUuid = producto.Uuid,
                            Nombre = producto.Nombre,
                            Descripcion = producto.Descripcion,
                            LogoUrl = producto.LogoUrl,
                            CodigoInterno = producto.CodigoInterno,
                            Cantidad = item.Cantidad,
                            PrecioUnitario = precio,
                            Subtotal = subtotalDetalle,
                            Observaciones = item.Observaciones,
                            FechaCreacion = DateTime.UtcNow
                        });
                    }

                    // ==========================================
                    // CALCULAR COMISIÓN
                    // ==========================================
                    var montoComisionPorcentaje = subtotal * (porcentajeComision / 100m);
                    var montoComision = Math.Round(montoComisionPorcentaje + comisionFija, 2);

                    if (montoComision > subtotal)
                    {
                        montoComision = subtotal;
                    }

                    var montoComercio = subtotal - montoComision;
                    var costoEnvio = configuracionCliente.TipoEntrega == TipoEntregaPedido.Domicilio
                        ? (configuracionPago.CompraMinimaEnvioGratis.HasValue && subtotal >= configuracionPago.CompraMinimaEnvioGratis.Value ? 0 : configuracionPago.CostoEnvio)
                        : 0;

                    montoComercio += costoEnvio;

                    // ==========================================
                    // CREAR PEDIDO
                    // ==========================================
                    var pedido = new Pedido
                    {
                        Uuid = Guid.NewGuid(),
                        NumeroPedido = GenerarNumeroPedido(),
                        IdUsuario = idUsuario,
                        IdComercio = comercio.Id,
                        IdDireccionUsuario = direccion?.Id,
                        Estado = EstadoPedido.PendienteAprobacion,
                        EstadoPago = configuracionCliente.MetodoPago == MetodoPagoPedido.Transferencia
                            ? EstadoPagoPedido.PendienteComprobante
                            : EstadoPagoPedido.Pendiente,
                        MetodoPago = configuracionCliente.MetodoPago,
                        TipoEntrega = configuracionCliente.TipoEntrega,
                        Subtotal = subtotal,
                        CostoEnvio = costoEnvio,
                        Total = subtotal + costoEnvio,
                        PorcentajeComision = porcentajeComision,
                        ComisionFija = comisionFija,
                        MontoComision = montoComision,
                        MontoComercio = montoComercio,
                        ComercioNombre = comercio.Nombre,
                        ComercioLogoUrl = comercio.LogoUrl,
                        ClienteNombre = usuario.Nombre,
                        ClienteEmail = usuario.Email,
                        ObservacionesCliente = string.IsNullOrWhiteSpace(configuracionCliente.Observaciones)
                            ? null
                            : configuracionCliente.Observaciones.Trim(),
                        FechaCreacion = DateTime.UtcNow,
                        Detalles = detalles
                    };

                    // SNAPSHOT DIRECCIÓN
                    if (direccion != null)
                    {
                        pedido.DireccionAlias = direccion.Alias;
                        pedido.DireccionCalle = direccion.Calle;
                        pedido.DireccionNumeroExterior = direccion.NumeroExterior;
                        pedido.DireccionNumeroInterior = direccion.NumeroInterior;
                        pedido.DireccionColonia = direccion.Colonia;
                        pedido.DireccionCodigoPostal = direccion.CodigoPostal;
                        pedido.DireccionEstado = direccion.Estado;
                        pedido.DireccionMunicipio = direccion.Municipio;
                        pedido.DireccionLatitud = direccion.Latitud;
                        pedido.DireccionLongitud = direccion.Longitud;
                        pedido.DireccionReferencias = direccion.Referencias;
                        pedido.TelefonoEntrega = direccion.Telefono;
                    }

                    // SNAPSHOT CUENTA BANCARIA
                    if (cuenta != null)
                    {
                        pedido.Banco = cuenta.Banco;
                        pedido.Beneficiario = cuenta.Beneficiario;
                        pedido.NumeroCuenta = cuenta.NumeroCuenta;
                        pedido.Clabe = cuenta.Clabe;
                        pedido.NumeroTarjeta = cuenta.NumeroTarjeta;
                        pedido.InstruccionesTransferencia = configuracionPago.InstruccionesTransferencia;
                    }

                    // HISTORIAL INICIAL
                    pedido.HistorialEstados.Add(new PedidoHistorialEstado
                    {
                        Uuid = Guid.NewGuid(),
                        EstadoAnterior = null,
                        EstadoNuevo = EstadoPedido.PendienteAprobacion,
                        IdUsuarioCambio = idUsuario,
                        Comentario = "Pedido creado por el cliente.",
                        FechaCreacion = DateTime.UtcNow
                    });

                    pedidos.Add(pedido);

                    response.Pedidos.Add(new PedidoCheckoutResponseDto
                    {
                        Uuid = pedido.Uuid,
                        NumeroPedido = pedido.NumeroPedido,
                        ComercioUuid = comercio.Uuid,
                        Comercio = comercio.Nombre,
                        Total = pedido.Total,
                        Estado = (int)pedido.Estado,
                        EstadoPago = (int)pedido.EstadoPago,
                        MetodoPago = (int)pedido.MetodoPago,
                        TipoEntrega = (int)pedido.TipoEntrega,
                        RequiereComprobante = pedido.MetodoPago == MetodoPagoPedido.Transferencia
                    });

                    response.TotalGeneral += pedido.Total;
                }

                // ==========================================
                // GUARDAR TODO EN UNA TRANSACCIÓN
                // ==========================================
                response.TotalPedidos = pedidos.Count;
                var responseJson = System.Text.Json.JsonSerializer.Serialize(response);

                await _repository.GuardarCheckoutAsync(
                    pedidos,
                    productosActualizarStock,
                    carrito,
                    idempotenciaRegistro,
                    responseJson,
                    cancellationToken
                );

                // ==========================================
                // NOTIFICACIONES (DESACOPLADAS DEL ÉXITO DEL PEDIDO)
                // ==========================================
                foreach (var pedidoCreado in pedidos)
                {
                    try
                    {
                        await _notificaciones.NotificarComercioAsync(
                            pedidoCreado,
                            TipoNotificacionPedido.PedidoCreado,
                            "Nuevo pedido",
                            $"{pedidoCreado.ClienteNombre} creó el pedido {pedidoCreado.NumeroPedido} por {pedidoCreado.Total:C}."
                        );
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogError(
                            notifEx,
                            "Error al notificar al comercio para el pedido confirmado {NumeroPedido}. El pedido se mantiene válido.",
                            pedidoCreado.NumeroPedido
                        );
                    }
                }

                return ApiResponse<ConfirmarCheckoutResponseDto>.Success(
                    response,
                    pedidos.Count == 1
                        ? "Pedido creado correctamente."
                        : $"{pedidos.Count} pedidos creados correctamente."
                );
            }
            catch (InvalidOperationException invEx)
            {
                return ApiResponse<ConfirmarCheckoutResponseDto>.Error("400", invEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al procesar checkout para el usuario {IdUsuario}",
                    _jwtContext.GetUserId()
                );

                return ApiResponse<ConfirmarCheckoutResponseDto>.Error("500", ex.Message);
            }
        }
    }
}
