using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.Models;

namespace AdLocalAPI.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> CrearAsync(RefreshToken token);
        Task<RefreshToken?> ObtenerPorHashConUsuarioAsync(string tokenHash);
        Task<RefreshToken?> ObtenerPorHashAsync(string tokenHash);
        Task<RefreshToken?> ObtenerPorIdYUsuarioAsync(long sesionId, long usuarioId);
        Task<List<RefreshToken>> ObtenerActivosPorUsuarioAsync(long usuarioId);
        Task ActualizarAsync(RefreshToken token);
        Task RotarTokenAsync(RefreshToken tokenActual, RefreshToken nuevoToken);
        Task RevocarTodosPorUsuarioAsync(long usuarioId, string razon, string? ip);
    }
}
