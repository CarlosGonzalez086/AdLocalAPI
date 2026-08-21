using AdLocalAPI.Data;
using AdLocalAPI.DTOs.Carrito;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class CarritoRepository : ICarritoRepository
    {
        private readonly AppDbContext _context;

        public CarritoRepository(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // CARRITO ACTIVO
        // ============================================================

        public async Task<Carrito?> ObtenerCarritoActivoAsync(
            long idUsuario)
        {
            return await _context.Carritos
                .FirstOrDefaultAsync(x =>
                    x.IdUsuario == idUsuario &&
                    x.Activo
                );
        }

        // ============================================================
        // DETALLE POR UUID
        // ============================================================

        public async Task<CarritoDetalle?> ObtenerDetallePorUuidAsync(
            long idUsuario,
            Guid detalleUuid)
        {
            return await (
                from detalle in _context.CarritoDetalles

                join carrito in _context.Carritos
                    on detalle.IdCarrito equals carrito.Id

                where
                    detalle.Uuid == detalleUuid &&
                    detalle.Activo &&
                    carrito.Activo &&
                    carrito.IdUsuario == idUsuario

                select detalle
            )
            .FirstOrDefaultAsync();
        }

        // ============================================================
        // DETALLE POR PRODUCTO
        // ============================================================

        public async Task<CarritoDetalle?> ObtenerDetalleProductoAsync(
            long idCarrito,
            long idProductoServicio)
        {
            /*
             * No filtramos por Activo.
             *
             * Si el usuario eliminó el producto anteriormente,
             * reutilizamos el registro.
             *
             * Esto evita romper el índice único:
             *
             * IdCarrito + IdProductoServicio
             */
            return await _context.CarritoDetalles
                .FirstOrDefaultAsync(x =>
                    x.IdCarrito == idCarrito &&
                    x.IdProductoServicio == idProductoServicio
                );
        }

        // ============================================================
        // DETALLES DEL CARRITO
        // ============================================================

        public async Task<List<CarritoDetalleResponseDto>>
            ObtenerDetallesAsync(long idCarrito)
        {
            return await (
                from detalle in _context.CarritoDetalles

                join producto in _context.ProductosServicios
                    .IgnoreQueryFilters()
                    on detalle.IdProductoServicio equals producto.Id

                join comercio in _context.Comercios
                    .IgnoreQueryFilters()
                    on producto.IdComercio equals comercio.Id

                where
                    detalle.IdCarrito == idCarrito &&
                    detalle.Activo

                orderby
                    comercio.Nombre,
                    detalle.FechaCreacion

                select new CarritoDetalleResponseDto
                {
                    Uuid = detalle.Uuid,

                    ProductoUuid = producto.Uuid,

                    // =========================
                    // COMERCIO
                    // =========================

                    IdComercio = comercio.Id,

                    ComercioUuid = comercio.Uuid,

                    ComercioNombre = comercio.Nombre,

                    ComercioLogoUrl = comercio.LogoUrl,

                    // =========================
                    // PRODUCTO
                    // =========================

                    Nombre = producto.Nombre,

                    Descripcion = producto.Descripcion,

                    LogoUrl = producto.LogoUrl,

                    Cantidad = detalle.Cantidad,

                    PrecioUnitario = detalle.PrecioUnitario,

                    Subtotal = detalle.Subtotal,

                    Observaciones = detalle.Observaciones,

                    Disponible =
                        producto.Activo &&
                        !producto.Eliminado &&
                        producto.Visible &&
                        producto.Disponible &&
                        comercio.Activo &&
                        comercio.Visible,

                    ManejaStock = producto.ManejaStock,

                    Stock = producto.Stock,

                    PermiteDomicilio = producto.PermiteDomicilio,

                    PermiteRecoger = producto.PermiteRecoger
                }
            )
            .ToListAsync();
        }

        // ============================================================
        // PRODUCTO POR UUID
        // ============================================================

        public async Task<ProductosServicios?> ObtenerProductoPorUuidAsync(
            Guid productoUuid)
        {
            return await _context.ProductosServicios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.Uuid == productoUuid
                );
        }

        // ============================================================
        // PRODUCTO POR ID
        // ============================================================

        public async Task<ProductosServicios?> ObtenerProductoPorIdAsync(
            long idProductoServicio)
        {
            return await _context.ProductosServicios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.Id == idProductoServicio
                );
        }

        // ============================================================
        // COMERCIO
        // ============================================================

        public async Task<Comercio?> ObtenerComercioAsync(
            long idComercio)
        {
            return await _context.Comercios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.Id == idComercio
                );
        }

        // ============================================================
        // CONFIGURACIÓN COMERCIO
        // ============================================================

        public async Task<ConfiguracionComercioPedido?>
            ObtenerConfiguracionComercioAsync(long idComercio)
        {
            return await _context.ConfiguracionComercioPedidos
                .FirstOrDefaultAsync(x =>
                    x.IdComercio == idComercio &&
                    x.Activo
                );
        }

        // ============================================================
        // CREAR CARRITO
        // ============================================================

        public async Task<Carrito> CrearCarritoAsync(
            Carrito carrito)
        {
            await _context.Carritos.AddAsync(
                carrito
            );

            await _context.SaveChangesAsync();

            return carrito;
        }

        // ============================================================
        // CREAR DETALLE
        // ============================================================

        public async Task<CarritoDetalle> CrearDetalleAsync(
            CarritoDetalle detalle)
        {
            await _context.CarritoDetalles.AddAsync(
                detalle
            );

            await _context.SaveChangesAsync();

            return detalle;
        }

        // ============================================================
        // ACTUALIZAR CARRITO
        // ============================================================

        public async Task ActualizarCarritoAsync(
            Carrito carrito)
        {
            _context.Carritos.Update(
                carrito
            );

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // ACTUALIZAR DETALLE
        // ============================================================

        public async Task ActualizarDetalleAsync(
            CarritoDetalle detalle)
        {
            _context.CarritoDetalles.Update(
                detalle
            );

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // DESACTIVAR DETALLE
        // ============================================================

        public async Task DesactivarDetalleAsync(
            CarritoDetalle detalle)
        {
            detalle.Activo = false;

            detalle.FechaActualizacion =
                DateTime.UtcNow;

            _context.CarritoDetalles.Update(
                detalle
            );

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // VACIAR CARRITO
        // ============================================================

        public async Task VaciarCarritoAsync(
            Carrito carrito)
        {
            var detalles =
                await _context.CarritoDetalles
                    .Where(x =>
                        x.IdCarrito == carrito.Id &&
                        x.Activo
                    )
                    .ToListAsync();

            foreach (var detalle in detalles)
            {
                detalle.Activo = false;

                detalle.FechaActualizacion =
                    DateTime.UtcNow;
            }

            carrito.Subtotal = 0;

            carrito.Activo = false;

            carrito.FechaActualizacion =
                DateTime.UtcNow;

            if (detalles.Count > 0)
            {
                _context.CarritoDetalles.UpdateRange(
                    detalles
                );
            }

            _context.Carritos.Update(
                carrito
            );

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // CALCULAR SUBTOTAL
        // ============================================================

        public async Task<decimal> CalcularSubtotalAsync(
            long idCarrito)
        {
            return await _context.CarritoDetalles
                .Where(x =>
                    x.IdCarrito == idCarrito &&
                    x.Activo
                )
                .SumAsync(x => x.Subtotal);
        }

        // ============================================================
        // CONTAR DETALLES ACTIVOS
        // ============================================================

        public async Task<int> ContarDetallesActivosAsync(
            long idCarrito)
        {
            return await _context.CarritoDetalles
                .CountAsync(x =>
                    x.IdCarrito == idCarrito &&
                    x.Activo
                );
        }
    }
}