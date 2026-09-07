using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Services
{
    public class NotificacionService : INotificacionService
    {
        private readonly INotificacionRepository _notificacionRepo;
        private readonly JwtContext _jwt;

        public NotificacionService(INotificacionRepository notificacionRepo, JwtContext jwt)
        {
            _notificacionRepo = notificacionRepo;
            _jwt = jwt;
        }

        public async Task<ApiResponse<ResumenNotificacionesDto>> ObtenerAsync(int limite = 20)
        {
            var idUsuario = _jwt.GetUserId();
            limite = Math.Clamp(limite, 1, 50);
            var rol = _jwt.GetUserRole();

            var notificaciones = await _notificacionRepo.ObtenerPorUsuarioAsync(idUsuario, rol, limite);
            var noLeidas = await _notificacionRepo.ContarNoLeidasAsync(idUsuario);

            return ApiResponse<ResumenNotificacionesDto>.Success(new ResumenNotificacionesDto
            {
                NoLeidas = noLeidas,
                Notificaciones = notificaciones
            });
        }

        public async Task<ApiResponse<object>> MarcarLeidaAsync(Guid uuid)
        {
            var idUsuario = _jwt.GetUserId();
            var success = await _notificacionRepo.MarcarLeidaAsync(uuid, idUsuario);
            if (!success)
            {
                return ApiResponse<object>.Error("404", "Notificación no encontrada.");
            }

            return ApiResponse<object>.Success(new { });
        }

        public async Task<ApiResponse<object>> MarcarTodasLeidasAsync()
        {
            var idUsuario = _jwt.GetUserId();
            await _notificacionRepo.MarcarTodasLeidasAsync(idUsuario);
            return ApiResponse<object>.Success(new { });
        }

        public async Task NotificarComercioAsync(
            Pedido pedido, TipoNotificacionPedido tipo, string titulo, string mensaje)
        {
            try
            {
                var destinatarios = await _notificacionRepo.ObtenerDestinatariosComercioAsync(pedido.IdComercio);
                await CrearAsync(destinatarios, pedido.Id, tipo, titulo, mensaje);
            }
            catch { }
        }

        public async Task NotificarClienteAsync(
            Pedido pedido, TipoNotificacionPedido tipo, string titulo, string mensaje)
        {
            try
            {
                await CrearAsync(new[] { pedido.IdUsuario }, pedido.Id, tipo, titulo, mensaje);
            }
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
            await _notificacionRepo.CrearNotificacionesAsync(notificaciones);
        }
    }
}
