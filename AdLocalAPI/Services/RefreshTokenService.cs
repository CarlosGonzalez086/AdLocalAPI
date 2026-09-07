using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AdLocalAPI.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private const string CookieName = "refreshToken";
        private const string AlternateCookieName = "adlocal_refresh_token";
        private const int DiasExpiracion = 30;

        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(IRefreshTokenRepository refreshTokenRepository, ILogger<RefreshTokenService> logger)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
        }

        public async Task<(string rawToken, RefreshToken tokenEntity)> GenerarRefreshTokenAsync(long usuarioId, string? ip = null)
        {
            var rawToken = GenerarTokenAleatorio();
            var tokenHash = CalcularHashSha256(rawToken);

            var entidad = new RefreshToken
            {
                UsuarioId = usuarioId,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(DiasExpiracion),
                CreatedByIp = ip
            };

            await _refreshTokenRepository.CrearAsync(entidad);

            return (rawToken, entidad);
        }

        public async Task<(bool success, string? error, string? newRawToken, Usuario? usuario)> RotarRefreshTokenAsync(string rawToken, string? ip = null)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return (false, "El token de refresco es requerido.", null, null);
            }

            var tokenHash = CalcularHashSha256(rawToken.Trim());

            var token = await _refreshTokenRepository.ObtenerPorHashConUsuarioAsync(tokenHash);

            if (token == null)
            {
                _logger.LogWarning("Intento de renovación con token de refresco inexistente desde IP {Ip}", ip);
                return (false, "Token de refresco inválido o inexistente.", null, null);
            }

            // Detección de reúso de token ya rotado (posible robo de sesión)
            if (token.IsRevoked)
            {
                if (!string.IsNullOrEmpty(token.ReplacedByTokenHash))
                {
                    _logger.LogWarning(
                        "ALERTA DE SEGURIDAD: Reúso de refresh token detectado para el usuario {UsuarioId} desde IP {Ip}. Revocando todas las sesiones.",
                        token.UsuarioId,
                        ip
                    );

                    await RevocarTodosPorUsuarioAsync(
                        token.UsuarioId,
                        $"Compromiso de seguridad detectado: Reúso de token ya rotado desde IP {ip}",
                        ip
                    );

                    return (false, "Sesión inválida o comprometida. Se han revocado todas las sesiones activas por seguridad.", null, null);
                }

                return (false, $"El token de refresco fue revocado: {token.ReasonRevoked ?? "Sesión finalizada"}.", null, null);
            }

            if (token.IsExpired)
            {
                return (false, "El token de refresco ha expirado. Por favor inicia sesión nuevamente.", null, null);
            }

            var usuario = token.Usuario;
            if (usuario == null || !usuario.Activo)
            {
                return (false, "El usuario asociado a la sesión no existe o está inactivo.", null, null);
            }

            // Rotar token: revocar el actual y crear uno nuevo vinculado
            var newRawToken = GenerarTokenAleatorio();
            var newHash = CalcularHashSha256(newRawToken);

            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ip;
            token.ReplacedByTokenHash = newHash;
            token.ReasonRevoked = "Rotado por renovación de sesión";

            var newToken = new RefreshToken
            {
                UsuarioId = token.UsuarioId,
                TokenHash = newHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(DiasExpiracion),
                CreatedByIp = ip
            };

            await _refreshTokenRepository.RotarTokenAsync(token, newToken);

            _logger.LogInformation("Refresh token rotado exitosamente para el usuario {UsuarioId}", token.UsuarioId);

            return (true, null, newRawToken, usuario);
        }

        public async Task RevocarTokenAsync(string rawToken, string razon, string? ip = null)
        {
            if (string.IsNullOrWhiteSpace(rawToken)) return;

            var hash = CalcularHashSha256(rawToken.Trim());
            var token = await _refreshTokenRepository.ObtenerPorHashAsync(hash);

            if (token != null && !token.IsRevoked)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ip;
                token.ReasonRevoked = razon;
                await _refreshTokenRepository.ActualizarAsync(token);

                _logger.LogInformation("Refresh token revocado para el usuario {UsuarioId}: {Razon}", token.UsuarioId, razon);
            }
        }

        public async Task RevocarTodosPorUsuarioAsync(long usuarioId, string razon, string? ip = null)
        {
            await _refreshTokenRepository.RevocarTodosPorUsuarioAsync(usuarioId, razon, ip);
            _logger.LogInformation("Se revocaron sesiones activas y se invalidaron access tokens previos para el usuario {UsuarioId}. Razón: {Razon}", usuarioId, razon);
        }

        public async Task<List<SesionActivaDto>> ObtenerSesionesActivasAsync(long usuarioId, string? rawTokenActual = null)
        {
            string? hashActual = !string.IsNullOrWhiteSpace(rawTokenActual) ? CalcularHashSha256(rawTokenActual.Trim()) : null;

            var tokens = await _refreshTokenRepository.ObtenerActivosPorUsuarioAsync(usuarioId);

            return tokens.Select(t => new SesionActivaDto
            {
                Id = t.Id,
                FechaCreacion = t.CreatedAt,
                FechaExpiracion = t.ExpiresAt,
                IpOrigen = t.CreatedByIp,
                EsSesionActual = !string.IsNullOrEmpty(hashActual) && t.TokenHash == hashActual
            }).ToList();
        }

        public async Task<bool> RevocarSesionPorIdAsync(long sesionId, long usuarioId, string razon, string? ip = null)
        {
            var token = await _refreshTokenRepository.ObtenerPorIdYUsuarioAsync(sesionId, usuarioId);

            if (token == null || token.IsRevoked)
            {
                return false;
            }

            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ip;
            token.ReasonRevoked = razon;

            await _refreshTokenRepository.ActualizarAsync(token);
            _logger.LogInformation("Sesión {SesionId} revocada manualmente para el usuario {UsuarioId}: {Razon}", sesionId, usuarioId, razon);
            return true;
        }

        public void EstablecerCookieRefreshToken(HttpResponse response, string rawToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(DiasExpiracion),
                Path = "/"
            };

            response.Cookies.Append(CookieName, rawToken, cookieOptions);
        }

        public void EliminarCookieRefreshToken(HttpResponse response)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            };

            response.Cookies.Delete(CookieName, cookieOptions);
            response.Cookies.Delete(AlternateCookieName, cookieOptions);
        }

        public string? ExtraerRefreshToken(HttpRequest request, string? bodyToken)
        {
            // 1. Probar cookies HttpOnly
            if (request.Cookies.TryGetValue(CookieName, out var cookieVal) && !string.IsNullOrWhiteSpace(cookieVal))
            {
                return cookieVal;
            }

            if (request.Cookies.TryGetValue(AlternateCookieName, out var altVal) && !string.IsNullOrWhiteSpace(altVal))
            {
                return altVal;
            }

            // 2. Probar body proporcionado
            if (!string.IsNullOrWhiteSpace(bodyToken))
            {
                return bodyToken;
            }

            // 3. Probar header personalizado
            if (request.Headers.TryGetValue("X-Refresh-Token", out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
            {
                return headerVal.ToString();
            }

            return null;
        }

        private static string GenerarTokenAleatorio()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static string CalcularHashSha256(string texto)
        {
            var bytes = Encoding.UTF8.GetBytes(texto);
            var hashBytes = SHA256.HashData(bytes);

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
