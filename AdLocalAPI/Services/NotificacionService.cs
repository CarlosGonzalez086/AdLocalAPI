using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Services
{
    public class NotificacionService : INotificacionService
    {
        private readonly AppDbContext _context;
        private readonly JwtContext _jwt;

        public NotificacionService(AppDbContext context, JwtContext jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        public async Task<ApiResponse<ResumenNotificacionesDto>> ObtenerAsync(int limite = 20)
        {
            var idUsuario = _jwt.GetUserId();
            limite = Math.Clamp(limite, 1, 50);
            var rol = _jwt.GetUserRole();

            var notificaciones = await (
                from notificacion in _context.Notificaciones.AsNoTracking()
                join pedido in _context.Pedidos.AsNoTracking()
                    on notificacion.IdReferencia equals pedido.Id into pedidos
                from pedido in pedidos.DefaultIfEmpty()
                where notificacion.IdUsuario == idUsuario && notificacion.Activo
                orderby notificacion.FechaCreacion descending
                select new NotificacionDto
                {
                    Uuid = notificacion.Uuid,
                    Titulo = notificacion.Titulo,
                    Mensaje = notificacion.Mensaje,
                    TipoNotificacion = notificacion.TipoNotificacion,
                    PedidoUuid = pedido == null ? null : pedido.Uuid,
                    Url = pedido == null
                        ? null
                        : rol == RolesUsuario.Cliente
                            ? "/usuario/pedidos?pedido=" + pedido.Uuid
                            : "/usuario/app/pedidos?pedido=" + pedido.Uuid,
                    Leida = notificacion.Leida,
                    FechaCreacion = notificacion.FechaCreacion
                }).Take(limite).ToListAsync();

            return ApiResponse<ResumenNotificacionesDto>.Success(new ResumenNotificacionesDto
            {
                NoLeidas = await _context.Notificaciones.AsNoTracking().CountAsync(x =>
                    x.IdUsuario == idUsuario && x.Activo && !x.Leida),
                Notificaciones = notificaciones
            });
        }

        public async Task<ApiResponse<object>> MarcarLeidaAsync(Guid uuid)
        {
            var idUsuario = _jwt.GetUserId();
            var notificacion = await _context.Notificaciones.AsTracking().FirstOrDefaultAsync(x =>
                x.Uuid == uuid && x.IdUsuario == idUsuario && x.Activo);
            if (notificacion == null) return ApiResponse<object>.Error("404", "Notificación no encontrada.");

            if (!notificacion.Leida)
            {
                notificacion.Leida = true;
                notificacion.FechaLectura = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return ApiResponse<object>.Success(new { });
        }

        public async Task<ApiResponse<object>> MarcarTodasLeidasAsync()
        {
            var idUsuario = _jwt.GetUserId();
            var pendientes = await _context.Notificaciones.AsTracking().Where(x =>
                x.IdUsuario == idUsuario && x.Activo && !x.Leida).ToListAsync();
            var fecha = DateTime.UtcNow;
            foreach (var notificacion in pendientes)
            {
                notificacion.Leida = true;
                notificacion.FechaLectura = fecha;
            }
            if (pendientes.Count > 0) await _context.SaveChangesAsync();
            return ApiResponse<object>.Success(new { });
        }

        public async Task NotificarComercioAsync(
            Pedido pedido, TipoNotificacionPedido tipo, string titulo, string mensaje)
        {
            try
            {
                var destinatarios = await _context.Usuarios.AsNoTracking()
                    .Where(x => x.Activo &&
                        x.ComercioId == pedido.IdComercio)
                    .Select(x => x.Id).Distinct().ToListAsync();

                var propietario = await _context.Comercios.AsNoTracking()
                    .Where(x => x.Id == pedido.IdComercio).Select(x => x.IdUsuario)
                    .FirstOrDefaultAsync();
                if (propietario > 0 && !destinatarios.Contains(propietario)) destinatarios.Add(propietario);

                await CrearAsync(destinatarios, pedido.Id, tipo, titulo, mensaje);
            }
            catch { }
        }

        public async Task NotificarClienteAsync(
            Pedido pedido, TipoNotificacionPedido tipo, string titulo, string mensaje)
        {
            try { await CrearAsync(new[] { pedido.IdUsuario }, pedido.Id, tipo, titulo, mensaje); }
            catch { }
        }

        private async Task CrearAsync(
            IEnumerable<long> destinatarios, long idPedido, TipoNotificacionPedido tipo,
            string titulo, string mensaje)
        {
            var notificaciones = destinatarios.Distinct().Select(idUsuario => new Notificacion
            {
                Uuid = Guid.NewGuid(),
                IdUsuario = idUsuario,
                Titulo = titulo,
                Mensaje = mensaje,
                TipoNotificacion = (int)tipo,
                IdReferencia = idPedido,
                TipoReferencia = "Pedido",
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            }).ToList();
            if (notificaciones.Count == 0) return;
            await _context.Notificaciones.AddRangeAsync(notificaciones);
            await _context.SaveChangesAsync();
        }
    }
}
