using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class NotificacionRepository : INotificacionRepository
    {
        private readonly AppDbContext _context;

        public NotificacionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<NotificacionDto>> ObtenerPorUsuarioAsync(long idUsuario, string? rol, int limite)
        {
            return await (
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
        }

        public async Task<int> ContarNoLeidasAsync(long idUsuario)
        {
            return await _context.Notificaciones.AsNoTracking().CountAsync(x =>
                x.IdUsuario == idUsuario && x.Activo && !x.Leida);
        }

        public async Task<bool> MarcarLeidaAsync(Guid uuid, long idUsuario)
        {
            var notificacion = await _context.Notificaciones.FirstOrDefaultAsync(x =>
                x.Uuid == uuid && x.IdUsuario == idUsuario && x.Activo);
            if (notificacion == null) return false;

            if (!notificacion.Leida)
            {
                notificacion.Leida = true;
                notificacion.FechaLectura = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<int> MarcarTodasLeidasAsync(long idUsuario)
        {
            var pendientes = await _context.Notificaciones.Where(x =>
                x.IdUsuario == idUsuario && x.Activo && !x.Leida).ToListAsync();
            var fecha = DateTime.UtcNow;
            foreach (var notificacion in pendientes)
            {
                notificacion.Leida = true;
                notificacion.FechaLectura = fecha;
            }
            if (pendientes.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
            return pendientes.Count;
        }

        public async Task<List<long>> ObtenerDestinatariosComercioAsync(long comercioId)
        {
            var destinatarios = await _context.Usuarios.AsNoTracking()
                .Where(x => x.Activo && x.ComercioId == comercioId)
                .Select(x => x.Id).Distinct().ToListAsync();

            var propietario = await _context.Comercios.AsNoTracking()
                .Where(x => x.Id == comercioId).Select(x => x.IdUsuario)
                .FirstOrDefaultAsync();
            if (propietario > 0 && !destinatarios.Contains(propietario))
            {
                destinatarios.Add(propietario);
            }

            return destinatarios;
        }

        public async Task CrearNotificacionesAsync(IEnumerable<Notificacion> notificaciones)
        {
            var list = notificaciones.ToList();
            if (list.Count == 0) return;
            await _context.Notificaciones.AddRangeAsync(list);
            await _context.SaveChangesAsync();
        }
    }
}
