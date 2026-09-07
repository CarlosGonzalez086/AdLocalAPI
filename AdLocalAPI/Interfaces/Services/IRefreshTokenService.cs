using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Http;

namespace AdLocalAPI.Interfaces.Services
{
    public interface IRefreshTokenService
    {
        Task<(string rawToken, RefreshToken tokenEntity)> GenerarRefreshTokenAsync(long usuarioId, string? ip = null);

        Task<(bool success, string? error, string? newRawToken, Usuario? usuario)> RotarRefreshTokenAsync(string rawToken, string? ip = null);

        Task RevocarTokenAsync(string rawToken, string razon, string? ip = null);

        Task RevocarTodosPorUsuarioAsync(long usuarioId, string razon, string? ip = null);

        Task<List<SesionActivaDto>> ObtenerSesionesActivasAsync(long usuarioId, string? rawTokenActual = null);

        Task<bool> RevocarSesionPorIdAsync(long sesionId, long usuarioId, string razon, string? ip = null);

        void EstablecerCookieRefreshToken(HttpResponse response, string rawToken);

        void EliminarCookieRefreshToken(HttpResponse response);

        string? ExtraerRefreshToken(HttpRequest request, string? bodyToken);
    }
}
