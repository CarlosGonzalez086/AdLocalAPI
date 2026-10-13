using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AdLocalAPI.Models;
using AdLocalAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Helpers
{
    public static class TokenSecurityValidator
    {
        public static JwtBearerEvents CrearEventos() => new()
        {
            OnTokenValidated = ValidarContextoAsync,
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(ApiResponse.Error("401", "Sesión no válida o ausente"));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(ApiResponse.Error("403", "No tienes permiso para realizar esta operación"));
            }
        };

        public static async Task ValidarContextoAsync(TokenValidatedContext context)
        {
            var idClaim = context.Principal?.FindFirst("id")?.Value;
            if (!long.TryParse(idClaim, out var id) || id <= 0)
            {
                context.Fail("Identidad de sesión inválida.");
                return;
            }
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id,
                context.HttpContext.RequestAborted);
            var (valid, error) = ValidarUsuarioYClaims(usuario, context.Principal);
            if (!valid) context.Fail(error!);
        }

        /// <summary>
        /// Valida en tiempo de ejecución las condiciones críticas de seguridad para un token JWT validado criptográficamente:
        /// 1. El usuario debe existir y encontrarse activo.
        /// 2. El token no debe haber sido emitido antes de la fecha de revocación global del usuario (TokensRevocadosAntesDe).
        /// 3. El rol incluido en los claims del token debe coincidir con el rol actual del usuario en la base de datos.
        /// </summary>
        public static (bool IsValid, string? ErrorMessage) ValidarUsuarioYClaims(Usuario? user, ClaimsPrincipal? principal)
        {
            if (user == null || !user.Activo)
            {
                return (false, "Usuario inactivo, bloqueado o inexistente.");
            }

            if (principal?.Identity?.IsAuthenticated != true ||
                !long.TryParse(principal.FindFirst("id")?.Value, out var id) || id <= 0 || id != user.Id)
                return (false, "Identidad de sesión inválida.");

            if (principal.FindAll("id").Count() != 1 ||
                principal.FindAll(ClaimTypes.Role).Count() != 1 ||
                principal.FindAll("rol").Count() != 1 ||
                principal.FindAll(JwtRegisteredClaimNames.Iat).Count() != 1)
                return (false, "La sesión contiene claims de seguridad ambiguos.");

            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            var customRole = principal.FindFirst("rol")?.Value;
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(customRole) ||
                !string.Equals(role, customRole, StringComparison.Ordinal) ||
                role is not ("Admin" or "Comercio" or "Colaborador" or "Cliente"))
                return (false, "Rol de sesión inválido.");

            if (!long.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Iat)?.Value, out var issuedAt) ||
                issuedAt < 0 || issuedAt > DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds())
                return (false, "Fecha de emisión de sesión inválida.");

            // 1. Invalidar token si fue emitido antes de la última revocación masiva (por cambio de password, rol o bloqueo)
            if (user.TokensRevocadosAntesDe.HasValue)
            {
                var issuedAtClaim = principal?.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
                if (long.TryParse(issuedAtClaim, out var iatEpoch))
                {
                    var tokenIssuedAt = DateTimeOffset.FromUnixTimeSeconds(iatEpoch).UtcDateTime;
                    if (tokenIssuedAt < user.TokensRevocadosAntesDe.Value.AddSeconds(-2))
                    {
                        return (false, "La sesión ha sido revocada. Por favor inicie sesión nuevamente.");
                    }
                }
            }

            // 2. Invalidar token si el rol del claim ya no coincide con el rol real en base de datos
            var tokenRole = principal?.FindFirst(ClaimTypes.Role)?.Value
                ?? principal?.FindFirst("rol")?.Value;

            if (!string.IsNullOrEmpty(tokenRole) && !string.Equals(tokenRole, user.Rol, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "El rol del usuario ha sido modificado. Es necesario renovar las credenciales.");
            }

            return (true, null);
        }
    }
}
