using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface ISuscripcionRepository
    {
        Task CrearAsync(Suscripcion suscripcion);
        Task ActualizarAsync(Suscripcion suscripcion);
        Task<Suscripcion?> GetActivaByUsuario(long usuarioId);
        Task<Suscripcion?> ObtenerActiva(int usuarioId);
        Task<Suscripcion?> ObtenerPorStripeId(string stripeSubscriptionId);
        Task<List<Suscripcion>> ObtenerHistorial(int usuarioId);
        Task EliminarAsync(int suscripcionId);
        Task<bool> ExistePorSessionAsync(string sessionId);
        Task<Suscripcion?> GetActivaByUsuarioAsync(long usuarioId);
        Task<List<Suscripcion>> ObtenerParaAutoRenovacionAsync(DateTime fecha);
        Task<(int total, List<Suscripcion> data)> ObtenerTodasAsync(int page, int pageSize);
        Task<List<SuscripcionPorPlanDto>> ObtenerConteoPorPlan();
        Task<int> SuscripcionesUltimaSemana();
        Task<int> SuscripcionesUltimosTresMeses();
        Task<string> ObtenerBadgeTextoUsuarioAsync(long usuarioId);
        Task<List<Suscripcion>> ObtenerActivasConStripeAsync();
    }
}
