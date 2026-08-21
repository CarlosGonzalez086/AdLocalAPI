using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class PedidoComercioRepository : IPedidoComercioRepository
    {
        private readonly AppDbContext _context;

        public PedidoComercioRepository(AppDbContext context) => _context = context;

        public async Task<List<ComercioPedidoSelectorDto>> ObtenerComerciosAsync(
            long idUsuario, string rol)
        {
            if (rol.Equals(RolesUsuario.Colaborador, StringComparison.OrdinalIgnoreCase))
            {
                return await (
                    from usuario in _context.Usuarios
                    join comercio in _context.Comercios on usuario.ComercioId equals comercio.Id
                    where usuario.Id == idUsuario && comercio.Activo
                    select new ComercioPedidoSelectorDto
                    {
                        Id = comercio.Id,
                        Uuid = comercio.Uuid,
                        Nombre = comercio.Nombre
                    }).ToListAsync();
            }

            return await _context.Comercios.AsNoTracking()
                .Where(x => x.IdUsuario == idUsuario && x.Activo)
                .OrderBy(x => x.Nombre)
                .Select(x => new ComercioPedidoSelectorDto
                {
                    Id = x.Id,
                    Uuid = x.Uuid,
                    Nombre = x.Nombre
                }).ToListAsync();
        }

        public async Task<bool> PuedeGestionarAsync(
            long idUsuario, string rol, long idComercio)
        {
            if (rol.Equals(RolesUsuario.Colaborador, StringComparison.OrdinalIgnoreCase))
            {
                return await _context.Usuarios.AsNoTracking().AnyAsync(x =>
                    x.Id == idUsuario && x.ComercioId == idComercio && x.Activo);
            }

            return await _context.Comercios.AsNoTracking().AnyAsync(x =>
                x.Id == idComercio && x.IdUsuario == idUsuario && x.Activo);
        }

        public async Task<PedidosComercioDashboardDto> ObtenerDashboardAsync(long idComercio)
        {
            var zona = ObtenerZonaHorariaMexico();
            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
            var hoyLocal = DateTime.SpecifyKind(ahoraLocal.Date, DateTimeKind.Unspecified);
            var semanaLocal = hoyLocal.AddDays(-(((int)hoyLocal.DayOfWeek + 6) % 7));
            var inicioHoy = TimeZoneInfo.ConvertTimeToUtc(hoyLocal, zona);
            var inicioSemana = TimeZoneInfo.ConvertTimeToUtc(semanaLocal, zona);
            var query = _context.Pedidos.AsNoTracking().Where(x => x.IdComercio == idComercio);

            var ventasSemana = await query.Where(x =>
                    x.EstadoPago == EstadoPagoPedido.Pagado && x.FechaCreacion >= inicioSemana)
                .Select(x => new { x.FechaCreacion, x.Total })
                .ToListAsync();

            var ventasPorFecha = ventasSemana
                .GroupBy(x => TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(x.FechaCreacion, DateTimeKind.Utc), zona).Date)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => new { Total = grupo.Sum(x => x.Total), Pedidos = grupo.Count() });

            var nombresDia = new[]
            {
                "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"
            };

            var dashboard = new PedidosComercioDashboardDto
            {
                VentasHoy = await query.Where(x =>
                    x.EstadoPago == EstadoPagoPedido.Pagado && x.FechaCreacion >= inicioHoy)
                    .SumAsync(x => (decimal?)x.Total) ?? 0,
                VentasSemana = ventasSemana.Sum(x => x.Total),
                PedidosHoy = await query.CountAsync(x => x.FechaCreacion >= inicioHoy),
                PendientesAprobacion = await query.CountAsync(x => x.Estado == EstadoPedido.PendienteAprobacion),
                ComprobantesPendientes = await query.CountAsync(x =>
                    x.EstadoPago == EstadoPagoPedido.PendienteVerificacion)
            };

            for (var index = 0; index < 7; index++)
            {
                var fecha = semanaLocal.Date.AddDays(index);
                ventasPorFecha.TryGetValue(fecha, out var venta);
                dashboard.VentasPorDia.Add(new VentaDiaComercioDto
                {
                    Fecha = fecha,
                    Dia = nombresDia[index],
                    Total = venta?.Total ?? 0,
                    Pedidos = venta?.Pedidos ?? 0
                });
            }

            return dashboard;
        }

        public async Task<PagedResponse<PedidoComercioListadoDto>> ObtenerPedidosAsync(
            long idComercio, int page, int pageSize, EstadoPedido? estado)
        {
            var query = _context.Pedidos.AsNoTracking().Where(x => x.IdComercio == idComercio);
            if (estado.HasValue) query = query.Where(x => x.Estado == estado.Value);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.FechaCreacion)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new PedidoComercioListadoDto
                {
                    Uuid = x.Uuid,
                    NumeroPedido = x.NumeroPedido,
                    ClienteNombre = x.ClienteNombre,
                    Total = x.Total,
                    Estado = x.Estado,
                    EstadoPago = x.EstadoPago,
                    MetodoPago = x.MetodoPago,
                    TipoEntrega = x.TipoEntrega,
                    TotalProductos = x.Detalles.Sum(d => d.Cantidad),
                    FechaCreacion = x.FechaCreacion,
                    TieneComprobante = x.ComprobantePagoUrl != null
                }).ToListAsync();

            foreach (var item in items)
                item.AccionesDisponibles = ObtenerAcciones(item.Estado, item.TipoEntrega);

            return new PagedResponse<PedidoComercioListadoDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Items = items
            };
        }

        public async Task<PedidoComercioDetalleDto?> ObtenerDetalleAsync(
            long idComercio, Guid pedidoUuid)
        {
            var dto = await _context.Pedidos.AsNoTracking()
                .Where(x => x.IdComercio == idComercio && x.Uuid == pedidoUuid)
                .Select(x => new PedidoComercioDetalleDto
                {
                    Uuid = x.Uuid,
                    NumeroPedido = x.NumeroPedido,
                    ClienteNombre = x.ClienteNombre,
                    ClienteEmail = x.ClienteEmail,
                    TelefonoEntrega = x.TelefonoEntrega,
                    Total = x.Total,
                    Estado = x.Estado,
                    EstadoPago = x.EstadoPago,
                    MetodoPago = x.MetodoPago,
                    TipoEntrega = x.TipoEntrega,
                    TotalProductos = x.Detalles.Sum(d => d.Cantidad),
                    FechaCreacion = x.FechaCreacion,
                    FechaComprobantePago = x.FechaComprobantePago,
                    TieneComprobante = x.ComprobantePagoUrl != null,
                    ObservacionesCliente = x.ObservacionesCliente,
                    Direccion = x.TipoEntrega == TipoEntregaPedido.Domicilio
                        ? x.DireccionCalle + " " + x.DireccionNumeroExterior + ", " +
                          x.DireccionColonia + ", " + x.DireccionMunicipio + ", " + x.DireccionEstado
                        : null,
                    Productos = x.Detalles.OrderBy(d => d.Id).Select(d => new PedidoComercioProductoDto
                    {
                        Uuid = d.Uuid,
                        Nombre = d.Nombre,
                        LogoUrl = d.LogoUrl,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Subtotal = d.Subtotal,
                        Observaciones = d.Observaciones
                    }).ToList(),
                    Historial = x.HistorialEstados.OrderByDescending(h => h.FechaCreacion)
                        .Select(h => new PedidoComercioHistorialDto
                        {
                            Estado = h.EstadoNuevo,
                            Comentario = h.Comentario,
                            Fecha = h.FechaCreacion
                        }).ToList()
                }).FirstOrDefaultAsync();

            if (dto != null) dto.AccionesDisponibles = ObtenerAcciones(dto.Estado, dto.TipoEntrega);
            return dto;
        }

        public Task<Pedido?> ObtenerPedidoTrackingAsync(long idComercio, Guid pedidoUuid) =>
            _context.Pedidos.AsTracking().FirstOrDefaultAsync(x =>
                x.IdComercio == idComercio && x.Uuid == pedidoUuid);

        public Task<ComprobantePago?> ObtenerComprobanteTrackingAsync(long idPedido) =>
            _context.ComprobantesPago.AsTracking()
                .Where(x => x.IdPedido == idPedido && x.Activo)
                .OrderByDescending(x => x.FechaCreacion).FirstOrDefaultAsync();

        public async Task GuardarEstadoAsync(Pedido pedido, PedidoHistorialEstado historial)
        {
            _context.Pedidos.Update(pedido);
            await _context.Set<PedidoHistorialEstado>().AddAsync(historial);
            await _context.SaveChangesAsync();
        }

        public async Task GuardarPagoAsync(Pedido pedido, ComprobantePago? comprobante)
        {
            _context.Pedidos.Update(pedido);
            if (comprobante != null) _context.ComprobantesPago.Update(comprobante);
            await _context.SaveChangesAsync();
        }

        public static List<EstadoPedido> ObtenerAcciones(EstadoPedido estado, TipoEntregaPedido entrega) =>
            estado switch
            {
                EstadoPedido.PendienteAprobacion => new() { EstadoPedido.Aprobado, EstadoPedido.Rechazado },
                EstadoPedido.Aprobado => new() { EstadoPedido.Preparando, EstadoPedido.Cancelado },
                EstadoPedido.Preparando => entrega == TipoEntregaPedido.Domicilio
                    ? new() { EstadoPedido.ListoParaEnviar, EstadoPedido.Cancelado }
                    : new() { EstadoPedido.ListoParaRecoger, EstadoPedido.Cancelado },
                EstadoPedido.ListoParaEnviar => new() { EstadoPedido.Enviado },
                EstadoPedido.Enviado => new() { EstadoPedido.Entregado },
                EstadoPedido.ListoParaRecoger => new() { EstadoPedido.Entregado },
                EstadoPedido.Entregado => new() { EstadoPedido.Completado },
                _ => new()
            };

        private static TimeZoneInfo ObtenerZonaHorariaMexico()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"); }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            }
        }
    }
}
