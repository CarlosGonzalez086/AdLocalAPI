using System.Data;
using Npgsql;
using AdLocalAPI.Helpers;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.EntityFrameworkCore;

using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.UsuarioCliente;

namespace AdLocalAPI.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly AppDbContext _context;

        public PedidoRepository(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<Carrito?> ObtenerCarritoActivoAsync(
            long idUsuario)
        {
            return await _context.Carritos
                .FirstOrDefaultAsync(x =>
                    x.IdUsuario == idUsuario &&
                    x.Activo
                );
        }

        public async Task<List<CheckoutCarritoItemDto>>
            ObtenerProductosCarritoAsync(
                long idCarrito)
        {
            return await (
                from detalle in _context.CarritoDetalles

                join producto in _context.ProductosServicios
                    .IgnoreQueryFilters()
                    on detalle.IdProductoServicio
                    equals producto.Id

                join comercio in _context.Comercios
                    on producto.IdComercio
                    equals comercio.Id

                where
                    detalle.IdCarrito == idCarrito &&
                    detalle.Activo

                select new CheckoutCarritoItemDto
                {
                    IdDetalleCarrito =
                        detalle.Id,

                    DetalleUuid =
                        detalle.Uuid,

                    IdProductoServicio =
                        detalle.IdProductoServicio,

                    Cantidad =
                        detalle.Cantidad,

                    Observaciones =
                        detalle.Observaciones,

                    Producto =
                        producto,

                    Comercio =
                        comercio
                }
            )
            .ToListAsync();
        }

        public async Task<Comercio?>
            ObtenerComercioPorUuidAsync(
                Guid uuid)
        {
            return await _context.Comercios
                .FirstOrDefaultAsync(x =>
                    x.Uuid == uuid &&
                    x.Activo
                );
        }

        public async Task<ConfiguracionPagoComercio?>
            ObtenerConfiguracionPagoAsync(
                long idComercio)
        {
            return await _context.ConfiguracionPagoComercios
                .FirstOrDefaultAsync(x =>
                    x.IdComercio == idComercio &&
                    x.Activo
                );
        }

        public async Task<CuentaBancariaComercio?>
            ObtenerCuentaPrincipalAsync(
                long idComercio)
        {
            return await _context.CuentasBancariasComercio
                .FirstOrDefaultAsync(x =>
                    x.IdComercio == idComercio &&
                    x.Activo &&
                    x.Principal
                );
        }

        public async Task<DireccionCheckoutDto?>
            ObtenerDireccionAsync(
                long idUsuario,
                Guid direccionUuid)
        {
            return await (
                from direccion in _context.DireccionesUsuarios

                join estado in _context.Estados
                    on direccion.IdEstado
                    equals estado.Id

                join municipio in _context.Municipios
                    on direccion.IdMunicipio
                    equals municipio.Id

                where
                    direccion.IdUsuario == idUsuario &&
                    direccion.Uuid == direccionUuid &&
                    direccion.Activo

                select new DireccionCheckoutDto
                {
                    Id =
                        direccion.Id,

                    Uuid =
                        direccion.Uuid,

                    Alias =
                        direccion.Alias,

                    Calle =
                        direccion.Calle,

                    NumeroExterior =
                        direccion.NumeroExterior,

                    NumeroInterior =
                        direccion.NumeroInterior,

                    Colonia =
                        direccion.Colonia,

                    CodigoPostal =
                        direccion.CodigoPostal,

                    Estado =
                        estado.EstadoNombre,

                    Municipio =
                        municipio.MunicipioNombre,

                    Latitud =
                        direccion.Latitud,

                    Longitud =
                        direccion.Longitud,

                    Referencias =
                        direccion.Referencias,

                    Telefono =
                        direccion.Telefono
                }
            )
            .FirstOrDefaultAsync();
        }

        public async Task<Usuario?> ObtenerUsuarioAsync(
            long idUsuario)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(x =>
                    x.Id == idUsuario &&
                    x.Activo
                );
        }

        public async Task<ConfiguracionSistema?>
            ObtenerConfiguracionAsync(
                string key)
        {
            return await _context.ConfiguracionSistema
                .FirstOrDefaultAsync(x =>
                    x.Key == key
                );
        }

        public async Task<CheckoutIdempotencia?> ObtenerIdempotenciaAsync(
            long idUsuario,
            string key)
        {
            return await _context.CheckoutIdempotencias
                .FirstOrDefaultAsync(x => x.IdUsuario == idUsuario && x.IdempotencyKey == key);
        }

        public async Task<CheckoutGuardado> GuardarCheckoutAsync(
            List<Pedido> pedidos,
            List<(long Id, int Cantidad, string Nombre)> productosActualizarStock,
            Carrito carrito,
            CheckoutIdempotencia idempotencia,
            string responseJson,
            string carritoVersion,
            CancellationToken cancellationToken = default)
        {
            // Solo errores PostgreSQL que garantizan rollback permiten repetir la transacción.
            for (var intento = 0; ; intento++)
            {
                try
                {
                    return await GuardarIntentoAsync(pedidos, productosActualizarStock, carrito,
                        idempotencia, responseJson, carritoVersion, cancellationToken);
                }
                catch (Exception ex) when (intento < 2 && EsConflictoTransitorio(ex))
                {
                    _context.ChangeTracker.Clear();
                    idempotencia.Id = 0;
                    foreach (var pedido in pedidos)
                    {
                        pedido.Id = 0;
                        foreach (var detalle in pedido.Detalles) detalle.Id = 0;
                        foreach (var historial in pedido.HistorialEstados) historial.Id = 0;
                    }
                }
            }
        }

        private static bool EsConflictoTransitorio(Exception ex)
        {
            for (Exception? causa = ex; causa != null; causa = causa.InnerException)
                if (causa is PostgresException postgres)
                    return postgres.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected ||
                        (postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                         postgres.ConstraintName == "IX_checkout_idempotencias_IdUsuario_IdempotencyKey");
            return false;
        }

        private async Task<CheckoutGuardado> GuardarIntentoAsync(
            List<Pedido> pedidos,
            List<(long Id, int Cantidad, string Nombre)> productosActualizarStock,
            Carrito carrito, CheckoutIdempotencia idempotencia, string responseJson,
            string carritoVersion, CancellationToken cancellationToken)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            try
            {
                var existente = await _context.CheckoutIdempotencias.AsTracking()
                    .SingleOrDefaultAsync(x => x.IdUsuario == idempotencia.IdUsuario &&
                        x.IdempotencyKey == idempotencia.IdempotencyKey, cancellationToken);
                if (existente != null)
                {
                    if (existente.RequestHash != null || existente.Status == "completed")
                        CheckoutOperacion.ValidarSolicitud(existente, idempotencia.RequestHash);
                    if (existente.Status == "completed")
                        return new CheckoutGuardado(existente.ResponseJson ?? throw new InvalidOperationException("Resultado guardado inválido."), false);
                    if (existente.Status == "processing" && existente.FechaCreacion > DateTime.UtcNow - CheckoutOperacion.EsperaLegado)
                        throw new CheckoutConflictException("Tu pedido ya se encuentra en procesamiento. Reintenta la misma operación en unos momentos.");
                    // Filas antiguas bloqueadas: se recuperan únicamente dentro de esta transacción.
                    existente.RequestHash = idempotencia.RequestHash;
                    existente.Status = "processing";
                    existente.FechaCreacion = DateTime.UtcNow;
                    idempotencia = existente;
                }
                else
                    _context.CheckoutIdempotencias.Add(idempotencia);
                // La unicidad se reclama dentro de la transacción; cualquier fallo revierte también esta fila.
                await _context.SaveChangesAsync(cancellationToken);

                var carritoActual = await _context.Carritos.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == carrito.Id && x.IdUsuario == idempotencia.IdUsuario, cancellationToken);
                if (carritoActual == null || !carritoActual.Activo || carritoActual.Uuid != carrito.Uuid)
                    throw new CheckoutConflictException("El carrito ya fue procesado. Revisa tus pedidos antes de intentar otra compra.");
                var itemsActuales = await ObtenerProductosCarritoAsync(carrito.Id);
                if (CheckoutOperacion.Carrito(itemsActuales) != carritoVersion)
                    throw new CheckoutValidationException("El carrito cambió mientras se confirmaba. Actualiza el checkout.");

                // ==========================================
                // 1. RE-VALIDAR PRECIOS Y DISPONIBILIDAD DENTRO DE LA TRANSACCIÓN
                // ==========================================
                var idsProductos = pedidos
                    .SelectMany(p => p.Detalles)
                    .Where(d => d.IdProductoServicio.HasValue)
                    .Select(d => d.IdProductoServicio!.Value)
                    .Distinct()
                    .ToList();

                var productosEnDb = await _context.ProductosServicios
                    .AsNoTracking()
                    .Where(p => idsProductos.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                foreach (var pedido in pedidos)
                {
                    foreach (var detalle in pedido.Detalles)
                    {
                        if (!detalle.IdProductoServicio.HasValue || !productosEnDb.TryGetValue(detalle.IdProductoServicio.Value, out var prodDb))
                        {
                            throw new CheckoutValidationException(
                                $"El producto '{detalle.Nombre}' ya no existe en el catálogo."
                            );
                        }

                        if (!prodDb.Activo || prodDb.Eliminado || !prodDb.Visible || !prodDb.Disponible)
                        {
                            throw new CheckoutValidationException(
                                $"El producto '{prodDb.Nombre}' ya no se encuentra disponible para compra."
                            );
                        }

                        if (!prodDb.Precio.HasValue || prodDb.Precio.Value != detalle.PrecioUnitario)
                        {
                            throw new CheckoutValidationException(
                                $"El precio de '{prodDb.Nombre}' ha cambiado. Por favor actualiza tu carrito antes de continuar."
                            );
                        }
                    }
                }

                // ==========================================
                // 2. ACTUALIZACIÓN ATÓMICA DE STOCK CONTRA CONCURRENCIA
                // ==========================================
                foreach (var itemStock in productosActualizarStock.OrderBy(x => x.Id))
                {
                    var filasAfectadas = await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE \"ProductosServicios\" SET \"stock\" = \"stock\" - {itemStock.Cantidad}, \"fecha_actualizacion\" = {DateTime.UtcNow} WHERE \"id\" = {itemStock.Id} AND \"maneja_stock\" = true AND \"stock\" >= {itemStock.Cantidad};",
                        cancellationToken
                    );

                    if (filasAfectadas == 0)
                    {
                        throw new CheckoutValidationException(
                            $"Stock insuficiente para el producto '{itemStock.Nombre}'. Por favor actualiza tu carrito."
                        );
                    }
                }

                // ==========================================
                // 3. GUARDAR PEDIDOS (con Detalles e Historial)
                // ==========================================
                await _context.Pedidos
                    .AddRangeAsync(pedidos, cancellationToken);

                // ==========================================
                // 4. CERRAR DETALLES DEL CARRITO
                // ==========================================
                var detallesCarrito =
                    await _context.CarritoDetalles
                        .Where(x =>
                            x.IdCarrito == carrito.Id &&
                            x.Activo
                        )
                        .ToListAsync(cancellationToken);

                foreach (var detalle in detallesCarrito)
                {
                    detalle.Activo = false;
                    detalle.FechaActualizacion = DateTime.UtcNow;
                }

                if (detallesCarrito.Count > 0)
                {
                    _context.CarritoDetalles
                        .UpdateRange(detallesCarrito);
                }

                // ==========================================
                // 5. CERRAR CARRITO
                // ==========================================
                carrito.Activo = false;
                carrito.FechaActualizacion = DateTime.UtcNow;
                _context.Carritos.Update(carrito);

                // ==========================================
                // 6. IDEMPOTENCIA (Actualizar a completado)
                // ==========================================
                if (idempotencia != null)
                {
                    idempotencia.Status = "completed";
                    idempotencia.ResponseJson = responseJson;
                    idempotencia.FechaCompletado = DateTime.UtcNow;
                    _context.CheckoutIdempotencias.Update(idempotencia);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new CheckoutGuardado(responseJson, true);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        public async Task<Pedido?> ObtenerPedidoClienteAsync(
            Guid pedidoUuid,
            long idUsuario)
        {
            return await _context.Pedidos
                .AsTracking()
                .FirstOrDefaultAsync(x =>
                    x.Uuid == pedidoUuid &&
                    x.IdUsuario == idUsuario
                );
        }

        public async Task GuardarComprobanteAsync(Pedido pedido, ComprobantePago comprobante)
        {
            _context.ChangeTracker.Clear();
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var actual = await _context.Pedidos.FromSqlInterpolated(
                    $"SELECT * FROM pedidos WHERE \"Id\" = {pedido.Id} AND \"IdUsuario\" = {pedido.IdUsuario} FOR UPDATE")
                    .AsTracking().SingleOrDefaultAsync();
                if (actual == null) throw new PedidoOperacionException("404", "No se encontró el pedido.");
                PedidoOperacion.Exigir(actual.MetodoPago == MetodoPagoPedido.Transferencia && !PedidoOperacion.Cerrado(actual.Estado) &&
                    actual.EstadoPago is EstadoPagoPedido.Pendiente or EstadoPagoPedido.Rechazado,
                    "El pedido cambió y ya no permite subir este comprobante.");
                var anteriores = await _context.ComprobantesPago.AsTracking().Where(x => x.IdPedido == actual.Id && x.Activo).ToListAsync();
                foreach (var anterior in anteriores) anterior.Activo = false;
                _context.ComprobantesPago.Add(comprobante);
                var pagoAnterior = actual.EstadoPago;
                actual.ComprobantePagoUrl = comprobante.ArchivoUrl;
                actual.FechaComprobantePago = comprobante.FechaCreacion;
                actual.EstadoPago = EstadoPagoPedido.PendienteVerificacion;
                actual.FechaActualizacion = DateTime.UtcNow; actual.Version++;
                _context.Add(new PedidoHistorialEstado { IdPedido = actual.Id, IdUsuarioCambio = actual.IdUsuario,
                    EstadoAnterior = actual.Estado, EstadoNuevo = actual.Estado, Evento = "Comprobante",
                    EstadoPagoAnterior = pagoAnterior, EstadoPagoNuevo = actual.EstadoPago });
                await _context.SaveChangesAsync(); await transaction.CommitAsync();
            }
            catch { await transaction.RollbackAsync(CancellationToken.None); _context.ChangeTracker.Clear(); throw; }
        }
        public Task<bool> ComprobanteRegistradoAsync(Guid uuid, long idUsuario) =>
            _context.ComprobantesPago.AsNoTracking().AnyAsync(x => x.Uuid == uuid && x.IdUsuario == idUsuario);
        public async Task<PagedResponse<PedidoClienteListadoDto>>
            ObtenerPedidosClienteAsync(long idUsuario, int page, int pageSize,
                EstadoPagoPedido? estadoPago)
        {
            var query = _context.Pedidos.AsNoTracking()
                .Where(x => x.IdUsuario == idUsuario);

            if (estadoPago.HasValue)
                query = query.Where(x => x.EstadoPago == estadoPago.Value);

            var totalItems = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.FechaCreacion)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new PedidoClienteListadoDto
                {
                    Uuid = x.Uuid,
                    NumeroPedido = x.NumeroPedido,
                    Comercio = x.ComercioNombre,
                    ComercioLogoUrl = x.ComercioLogoUrl,
                    Total = x.Total,
                    Subtotal = x.Subtotal,
                    CostoEnvio = x.CostoEnvio,
                    Estado = x.Estado,
                    EstadoPago = x.EstadoPago,
                    MetodoPago = x.MetodoPago,
                    TipoEntrega = x.TipoEntrega,
                    TotalProductos = x.Detalles.Sum(d => d.Cantidad),
                    FechaCreacion = x.FechaCreacion,
                    FechaComprobantePago = x.FechaComprobantePago,
                    PuedeSubirComprobante =
                        x.MetodoPago == MetodoPagoPedido.Transferencia &&
                        x.Estado != EstadoPedido.Cancelado &&
                        x.Estado != EstadoPedido.Rechazado &&
                        (x.EstadoPago == EstadoPagoPedido.PendienteComprobante ||
                         x.EstadoPago == EstadoPagoPedido.Rechazado)
                }).ToListAsync();

            return new PagedResponse<PedidoClienteListadoDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        public async Task<PedidoClienteDetalleDto?>
            ObtenerDetallePedidoClienteAsync(Guid pedidoUuid, long idUsuario)
        {
            return await _context.Pedidos.AsNoTracking()
                .Where(x => x.Uuid == pedidoUuid && x.IdUsuario == idUsuario)
                .Select(x => new PedidoClienteDetalleDto
                {
                    Reembolso = x.Reembolso == null ? null : new PedidoReembolsoDto
                    {
                        Uuid = x.Reembolso.Uuid, Monto = x.Reembolso.Monto, Metodo = x.Reembolso.Metodo,
                        Referencia = x.Reembolso.Referencia, Motivo = x.Reembolso.Motivo, FechaCreacion = x.Reembolso.FechaCreacion
                    },
                    Uuid = x.Uuid,
                    NumeroPedido = x.NumeroPedido,
                    Comercio = x.ComercioNombre,
                    ComercioLogoUrl = x.ComercioLogoUrl,
                    Total = x.Total,
                    Subtotal = x.Subtotal,
                    CostoEnvio = x.CostoEnvio,
                    Estado = x.Estado,
                    EstadoPago = x.EstadoPago,
                    MetodoPago = x.MetodoPago,
                    TipoEntrega = x.TipoEntrega,
                    TotalProductos = x.Detalles.Sum(d => d.Cantidad),
                    FechaCreacion = x.FechaCreacion,
                    FechaComprobantePago = x.FechaComprobantePago,
                    PuedeSubirComprobante =
                        x.MetodoPago == MetodoPagoPedido.Transferencia &&
                        x.Estado != EstadoPedido.Cancelado &&
                        x.Estado != EstadoPedido.Rechazado &&
                        (x.EstadoPago == EstadoPagoPedido.PendienteComprobante ||
                         x.EstadoPago == EstadoPagoPedido.Rechazado),
                    ObservacionesCliente = x.ObservacionesCliente,
                    Direccion = x.TipoEntrega == TipoEntregaPedido.Domicilio
                        ? x.DireccionCalle + " " + x.DireccionNumeroExterior +
                          (x.DireccionNumeroInterior == null ? "" : " Int. " + x.DireccionNumeroInterior) +
                          ", " + x.DireccionColonia + ", " + x.DireccionMunicipio +
                          ", " + x.DireccionEstado + " C.P. " + x.DireccionCodigoPostal
                        : null,
                    TelefonoEntrega = x.TelefonoEntrega,
                    Banco = x.Banco,
                    Beneficiario = x.Beneficiario,
                    NumeroCuenta = x.NumeroCuenta,
                    Clabe = x.Clabe,
                    NumeroTarjeta = x.NumeroTarjeta,
                    InstruccionesTransferencia = x.InstruccionesTransferencia,
                    Productos = x.Detalles.OrderBy(d => d.Id)
                        .Select(d => new PedidoClienteProductoDto
                        {
                            Uuid = d.Uuid,
                            ProductoUuid = d.ProductoUuid,
                            Nombre = d.Nombre,
                            LogoUrl = d.LogoUrl,
                            Cantidad = d.Cantidad,
                            PrecioUnitario = d.PrecioUnitario,
                            Subtotal = d.Subtotal,
                            Observaciones = d.Observaciones
                        }).ToList(),
                    Historial = x.HistorialEstados.Where(h => h.Evento != "Inventario")
                        .OrderByDescending(h => h.FechaCreacion)
                        .Select(h => new PedidoClienteHistorialDto
                        {
                            Evento = h.Evento,
                            EstadoPago = h.EstadoPagoNuevo,
                            Estado = h.EstadoNuevo,
                            Comentario = h.Comentario,
                            Fecha = h.FechaCreacion
                        }).ToList()
                }).FirstOrDefaultAsync();
        }
    }
}
