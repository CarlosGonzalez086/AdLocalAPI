using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface INotificacionRepository
    {
        Task<List<NotificacionDto>> ObtenerPorUsuarioAsync(long idUsuario, string? rol, int limite);
        Task<int> ContarNoLeidasAsync(long idUsuario);
        Task<bool> MarcarLeidaAsync(Guid uuid, long idUsuario);
        Task<int> MarcarTodasLeidasAsync(long idUsuario);
        Task<List<long>> ObtenerDestinatariosComercioAsync(long comercioId);
        Task CrearNotificacionesAsync(IEnumerable<Notificacion> notificaciones);
    }
}
