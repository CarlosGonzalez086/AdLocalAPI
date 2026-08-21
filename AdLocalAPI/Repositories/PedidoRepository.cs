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

        public async Task GuardarCheckoutAsync(
            List<Pedido> pedidos,
            List<ProductosServicios> productosActualizar,
            Carrito carrito)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // ==========================================
                // PEDIDOS
                // ==========================================

                await _context.Pedidos
                    .AddRangeAsync(pedidos);

                // ==========================================
                // STOCK
                // ==========================================

                if (productosActualizar.Count > 0)
                {
                    _context.ProductosServicios
                        .UpdateRange(
                            productosActualizar
                        );
                }

                // ==========================================
                // CERRAR DETALLES DEL CARRITO
                // ==========================================

                var detallesCarrito =
                    await _context.CarritoDetalles
                        .Where(x =>
                            x.IdCarrito == carrito.Id &&
                            x.Activo
                        )
                        .ToListAsync();

                foreach (var detalle in detallesCarrito)
                {
                    detalle.Activo = false;

                    detalle.FechaActualizacion =
                        DateTime.UtcNow;
                }

                if (detallesCarrito.Count > 0)
                {
                    _context.CarritoDetalles
                        .UpdateRange(
                            detallesCarrito
                        );
                }

                // ==========================================
                // CERRAR CARRITO
                // ==========================================

                carrito.Activo = false;

                carrito.FechaActualizacion =
                    DateTime.UtcNow;

                _context.Carritos.Update(
                    carrito
                );

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

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

        public async Task GuardarComprobanteAsync(
            Pedido pedido,
            ComprobantePago comprobante)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var comprobantesAnteriores =
                    await _context.ComprobantesPago
                        .Where(x =>
                            x.IdPedido == pedido.Id &&
                            x.Activo
                        )
                        .ToListAsync();

                foreach (var anterior in comprobantesAnteriores)
                {
                    anterior.Activo = false;
                }

                await _context.ComprobantesPago.AddAsync(comprobante);

                pedido.ComprobantePagoUrl = comprobante.ArchivoUrl;
                pedido.FechaComprobantePago = comprobante.FechaCreacion;
                pedido.EstadoPago = EstadoPagoPedido.PendienteVerificacion;
                pedido.FechaActualizacion = DateTime.UtcNow;

                _context.Pedidos.Update(pedido);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
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
                    Uuid = x.Uuid,
                    NumeroPedido = x.NumeroPedido,
                    Comercio = x.ComercioNombre,
                    ComercioLogoUrl = x.ComercioLogoUrl,
                    Total = x.Total,
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
                    Historial = x.HistorialEstados
                        .OrderByDescending(h => h.FechaCreacion)
                        .Select(h => new PedidoClienteHistorialDto
                        {
                            Estado = h.EstadoNuevo,
                            Comentario = h.Comentario,
                            Fecha = h.FechaCreacion
                        }).ToList()
                }).FirstOrDefaultAsync();
        }
    }
}
