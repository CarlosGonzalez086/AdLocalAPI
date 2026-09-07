using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AdLocalAPI.Models;

namespace AdLocalAPI.Helpers
{
    public static class TokenSecurityValidator
    {
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
