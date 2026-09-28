using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class TokenValidationSecurityTests
    {
        private ClaimsPrincipal CreatePrincipal(long userId, string role, DateTime issuedAt)
        {
            var iatEpoch = new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString();
            var claims = new List<Claim>
            {
                new Claim("id", userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("rol", role),
                new Claim(JwtRegisteredClaimNames.Iat, iatEpoch)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public void ValidarUsuarioYClaims_NullOrInactiveUser_ReturnsError()
        {
            var principal = CreatePrincipal(1, "Comercio", DateTime.UtcNow);

            // Caso 1: Usuario nulo (eliminado)
            var (valid1, error1) = TokenSecurityValidator.ValidarUsuarioYClaims(null, principal);
            Assert.False(valid1);
            Assert.Contains("inactivo", error1, StringComparison.OrdinalIgnoreCase);

            // Caso 2: Usuario inactivo (bloqueado)
            var userInactivo = new Usuario { Id = 1, Rol = "Comercio", Activo = false };
            var (valid2, error2) = TokenSecurityValidator.ValidarUsuarioYClaims(userInactivo, principal);
            Assert.False(valid2);
            Assert.Contains("inactivo", error2, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidarUsuarioYClaims_TokenIssuedBeforeRevocationCutoff_ReturnsError()
        {
            // El usuario cambió su contraseña a las 14:00 (TokensRevocadosAntesDe = 14:00)
            var revocationTime = DateTime.UtcNow;
            var user = new Usuario
            {
                Id = 2,
                Rol = "Comercio",
                Activo = true,
                TokensRevocadosAntesDe = revocationTime
            };

            // Token emitido 10 minutos antes (a las 13:50)
            var oldPrincipal = CreatePrincipal(2, "Comercio", revocationTime.AddMinutes(-10));

            // Act
            var (isValid, errorMessage) = TokenSecurityValidator.ValidarUsuarioYClaims(user, oldPrincipal);

            // Assert
            Assert.False(isValid);
            Assert.Contains("revocada", errorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidarUsuarioYClaims_TokenIssuedAfterRevocationCutoff_Succeeds()
        {
            var revocationTime = DateTime.UtcNow.AddMinutes(-30);
            var user = new Usuario
            {
                Id = 3,
                Rol = "Comercio",
                Activo = true,
                TokensRevocadosAntesDe = revocationTime
            };

            // Token emitido hace 5 minutos (después del evento de revocación)
            var freshPrincipal = CreatePrincipal(3, "Comercio", DateTime.UtcNow.AddMinutes(-5));

            // Act
            var (isValid, errorMessage) = TokenSecurityValidator.ValidarUsuarioYClaims(user, freshPrincipal);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void ValidarUsuarioYClaims_TokenRoleMismatchedWithDatabase_ReturnsError()
        {
            // El usuario fue degradado o cambiado de rol en base de datos de "Admin" a "Comercio"
            var user = new Usuario
            {
                Id = 4,
                Rol = "Comercio", // Rol real actual en DB
                Activo = true
            };

            // Token viejo que aún contiene el claim de "Admin"
            var adminClaimPrincipal = CreatePrincipal(4, "Admin", DateTime.UtcNow);

            // Act
            var (isValid, errorMessage) = TokenSecurityValidator.ValidarUsuarioYClaims(user, adminClaimPrincipal);

            // Assert: El servidor debe rechazar inmediatamente el token con rol no sincronizado
            Assert.False(isValid);
            Assert.Contains("rol del usuario ha sido modificado", errorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidarUsuarioYClaims_MatchingRoleAndActiveUser_Succeeds()
        {
            var user = new Usuario
            {
                Id = 5,
                Rol = "Cliente",
                Activo = true
            };

            var principal = CreatePrincipal(5, "Cliente", DateTime.UtcNow);

            // Act
            var (isValid, errorMessage) = TokenSecurityValidator.ValidarUsuarioYClaims(user, principal);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }
    }
}
