using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AdLocalAPI.Data;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class RefreshTokenServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private RefreshTokenService CreateService(AppDbContext context)
        {
            var repo = new RefreshTokenRepository(context);
            return new RefreshTokenService(repo, NullLogger<RefreshTokenService>.Instance);
        }

        private static string HashSha256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input.Trim());
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        [Fact]
        public async Task GenerarRefreshTokenAsync_CreatesHashedToken_InDatabase()
        {
            // Arrange
            using var context = CreateDbContext();
            var service = CreateService(context);

            var usuario = new Usuario
            {
                Id = 10,
                Nombre = "Comercio Test",
                Email = "comercio@test.com",
                PasswordHash = "hash123",
                Rol = "Comercio",
                Activo = true
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            // Act
            var (rawToken, tokenEntity) = await service.GenerarRefreshTokenAsync(usuario.Id, "192.168.1.1");

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(rawToken));
            Assert.NotNull(tokenEntity);
            Assert.Equal(usuario.Id, tokenEntity.UsuarioId);
            Assert.NotEqual(rawToken, tokenEntity.TokenHash);
            Assert.Equal(HashSha256(rawToken), tokenEntity.TokenHash);
            Assert.Equal(64, tokenEntity.TokenHash.Length);
            Assert.Equal("192.168.1.1", tokenEntity.CreatedByIp);
            Assert.True(tokenEntity.ExpiresAt > DateTime.UtcNow.AddDays(29));
            Assert.Null(tokenEntity.RevokedAt);

            // Verificar persistencia en base de datos
            var enDb = await context.RefreshTokens.FirstOrDefaultAsync(r => r.Id == tokenEntity.Id);
            Assert.NotNull(enDb);
            Assert.Equal(tokenEntity.TokenHash, enDb.TokenHash);
        }

        [Fact]
        public async Task RotarRefreshTokenAsync_ValidActiveToken_RotatesSuccessfully()
        {
            // Arrange
            using var context = CreateDbContext();
            var service = CreateService(context);

            var usuario = new Usuario
            {
                Id = 11,
                Nombre = "Usuario Rotacion",
                Email = "rotacion@test.com",
                PasswordHash = "hash123",
                Rol = "Cliente",
                Activo = true
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            var (rawToken1, tokenEntity1) = await service.GenerarRefreshTokenAsync(usuario.Id, "10.0.0.1");

            // Act
            var (success, error, newRawToken, userReturned) = await service.RotarRefreshTokenAsync(rawToken1, "10.0.0.2");

            // Assert
            Assert.True(success);
            Assert.Null(error);
            Assert.NotNull(newRawToken);
            Assert.NotEqual(rawToken1, newRawToken);
            Assert.NotNull(userReturned);
            Assert.Equal(usuario.Id, userReturned.Id);

            // Verificar token antiguo revocado con puntero al nuevo
            var oldInDb = await context.RefreshTokens.FindAsync(tokenEntity1.Id);
            Assert.NotNull(oldInDb);
            Assert.NotNull(oldInDb.RevokedAt);
            Assert.Equal("10.0.0.2", oldInDb.RevokedByIp);
            Assert.Equal("Rotado por renovación de sesión", oldInDb.ReasonRevoked);
            Assert.Equal(HashSha256(newRawToken), oldInDb.ReplacedByTokenHash);

            // Verificar nuevo token activo en BD
            var newInDb = await context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == HashSha256(newRawToken));
            Assert.NotNull(newInDb);
            Assert.Equal(usuario.Id, newInDb.UsuarioId);
            Assert.Null(newInDb.RevokedAt);
            Assert.True(newInDb.IsActive);
        }

        [Fact]
        public async Task RotarRefreshTokenAsync_ReusedRevokedToken_DetectsTheft_AndRevokesAllSessions()
        {
            // Arrange
            using var context = CreateDbContext();
            var service = CreateService(context);

            var usuario = new Usuario
            {
                Id = 12,
                Nombre = "Usuario Victima",
                Email = "victima@test.com",
                PasswordHash = "hash123",
                Rol = "Cliente",
                Activo = true
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            // Generar sesión original y una sesión adicional en otro dispositivo
            var (rawTokenOriginal, _) = await service.GenerarRefreshTokenAsync(usuario.Id, "1.1.1.1");
            var (rawTokenOtroDispositivo, _) = await service.GenerarRefreshTokenAsync(usuario.Id, "2.2.2.2");

            // Rotación legítima (T1 -> T2)
            var (success1, _, rawToken2, _) = await service.RotarRefreshTokenAsync(rawTokenOriginal, "1.1.1.1");
            Assert.True(success1);
            Assert.NotNull(rawToken2);

            // Act: Intento de reusar T1 (el token original ya reemplazado por T2)
            var (successReuse, errorReuse, newRawTokenReuse, _) = await service.RotarRefreshTokenAsync(rawTokenOriginal, "66.66.66.66");

            // Assert
            Assert.False(successReuse);
            Assert.Null(newRawTokenReuse);
            Assert.Contains("comprometida", errorReuse, StringComparison.OrdinalIgnoreCase);

            // Verificar que TODAS las sesiones del usuario fueron revocadas (incluyendo T2 y el otro dispositivo)
            var sesionesUsuario = await context.RefreshTokens.Where(r => r.UsuarioId == usuario.Id).ToListAsync();
            Assert.NotEmpty(sesionesUsuario);
            foreach (var sesion in sesionesUsuario)
            {
                Assert.NotNull(sesion.RevokedAt);
                Assert.True(sesion.IsRevoked);
            }

            // Verificar que TokensRevocadosAntesDe fue marcado en el usuario
            var userInDb = await context.Usuarios.FindAsync(usuario.Id);
            Assert.NotNull(userInDb);
            Assert.NotNull(userInDb.TokensRevocadosAntesDe);
        }

        [Fact]
        public async Task RotarRefreshTokenAsync_ExpiredToken_Fails()
        {
            // Arrange
            using var context = CreateDbContext();
            var service = CreateService(context);

            var usuario = new Usuario
            {
                Id = 13,
                Nombre = "Usuario Expirado",
                Email = "expirado@test.com",
                PasswordHash = "hash123",
                Rol = "Cliente",
                Activo = true
            };
            context.Usuarios.Add(usuario);

            var rawToken = "raw-expired-token-123";
            var tokenHash = HashSha256(rawToken);

            var expiredToken = new RefreshToken
            {
                UsuarioId = usuario.Id,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow.AddDays(-35),
                ExpiresAt = DateTime.UtcNow.AddDays(-5), // Expirado hace 5 días
                CreatedByIp = "127.0.0.1"
            };
            context.RefreshTokens.Add(expiredToken);
            await context.SaveChangesAsync();

            // Act
            var (success, error, newToken, _) = await service.RotarRefreshTokenAsync(rawToken, "127.0.0.1");

            // Assert
            Assert.False(success);
            Assert.Null(newToken);
            Assert.Contains("expirado", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RotarRefreshTokenAsync_InactiveUser_Fails()
        {
            // Arrange
            using var context = CreateDbContext();
            var service = CreateService(context);

            var usuario = new Usuario
            {
                Id = 14,
                Nombre = "Usuario Bloqueado",
                Email = "bloqueado@test.com",
                PasswordHash = "hash123",
                Rol = "Cliente",
                Activo = false // Bloqueado/inactivo
            };
            context.Usuarios.Add(usuario);

            var (rawToken, _) = await service.GenerarRefreshTokenAsync(usuario.Id, "127.0.0.1");

            // Act
            var (success, error, _, _) = await service.RotarRefreshTokenAsync(rawToken, "127.0.0.1");

            // Assert
            Assert.False(success);
            Assert.Contains("inactivo", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RevocarTokenAsync_RevokesSpecificToken()
        {
            // Arrange
            using var context = CreateDbContext();
            var service = CreateService(context);

            var usuario = new Usuario
            {
                Id = 15,
                Nombre = "Usuario Logout",
                Email = "logout@test.com",
                PasswordHash = "hash123",
                Rol = "Cliente",
                Activo = true
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            var (rawToken, tokenEntity) = await service.GenerarRefreshTokenAsync(usuario.Id, "1.1.1.1");

            // Act
            await service.RevocarTokenAsync(rawToken, "Logout voluntario del usuario", "1.1.1.1");

            // Assert
            var inDb = await context.RefreshTokens.FindAsync(tokenEntity.Id);
            Assert.NotNull(inDb);
            Assert.NotNull(inDb.RevokedAt);
            Assert.Equal("Logout voluntario del usuario", inDb.ReasonRevoked);
        }

        [Fact]
        public async Task RevocarSesionPorIdAsync_RevokesTargetSession_ForCorrectUserOnly()
        {
            // Arrange
            using var context = CreateDbContext();
            var service = CreateService(context);

            var user1 = new Usuario { Id = 16, Nombre = "User Uno", Email = "u1@test.com", PasswordHash = "h", Rol = "Cliente", Activo = true };
            var user2 = new Usuario { Id = 17, Nombre = "User Dos", Email = "u2@test.com", PasswordHash = "h", Rol = "Cliente", Activo = true };
            context.Usuarios.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var (_, tokenU1) = await service.GenerarRefreshTokenAsync(user1.Id, "1.1.1.1");
            var (_, tokenU2) = await service.GenerarRefreshTokenAsync(user2.Id, "2.2.2.2");

            // Act 1: User 2 intenta revocar la sesión de User 1 (ID spoofing)
            var resultIntruso = await service.RevocarSesionPorIdAsync(tokenU1.Id, user2.Id, "Intento no autorizado", "2.2.2.2");

            // Assert 1: Debe fallar
            Assert.False(resultIntruso);
            var u1TokenNoTocado = await context.RefreshTokens.FindAsync(tokenU1.Id);
            Assert.NotNull(u1TokenNoTocado);
            Assert.Null(u1TokenNoTocado.RevokedAt);

            // Act 2: User 1 revoca su propia sesión
            var resultPropio = await service.RevocarSesionPorIdAsync(tokenU1.Id, user1.Id, "Revocada desde panel de sesiones", "1.1.1.1");

            // Assert 2: Debe tener éxito
            Assert.True(resultPropio);
            var u1TokenRevocado = await context.RefreshTokens.FindAsync(tokenU1.Id);
            Assert.NotNull(u1TokenRevocado);
            Assert.NotNull(u1TokenRevocado.RevokedAt);
            Assert.Equal("Revocada desde panel de sesiones", u1TokenRevocado.ReasonRevoked);
        }

        [Fact]
        public async Task ObtenerSesionesActivasAsync_ReturnsOnlyActiveSessions_AndIdentifiesCurrent()
        {
            // Arrange
            using var context = CreateDbContext();
            var service = CreateService(context);

            var usuario = new Usuario
            {
                Id = 18,
                Nombre = "User Sesiones",
                Email = "sesiones@test.com",
                PasswordHash = "h",
                Rol = "Cliente",
                Activo = true
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            var (rawTokenActual, _) = await service.GenerarRefreshTokenAsync(usuario.Id, "192.168.1.100");
            var (rawTokenSecundario, _) = await service.GenerarRefreshTokenAsync(usuario.Id, "192.168.1.200");
            var (rawTokenRevocado, tokenRevocado) = await service.GenerarRefreshTokenAsync(usuario.Id, "192.168.1.300");

            // Revocar el tercero
            await service.RevocarTokenAsync(rawTokenRevocado, "Revocada", "192.168.1.300");

            // Act
            var sesionesActivas = await service.ObtenerSesionesActivasAsync(usuario.Id, rawTokenActual);

            // Assert
            Assert.Equal(2, sesionesActivas.Count);
            Assert.Contains(sesionesActivas, s => s.EsSesionActual && s.IpOrigen == "192.168.1.100");
            Assert.Contains(sesionesActivas, s => !s.EsSesionActual && s.IpOrigen == "192.168.1.200");
            Assert.DoesNotContain(sesionesActivas, s => s.Id == tokenRevocado.Id);
        }

        [Fact]
        public async Task RevocarTodosPorUsuarioAsync_RevokesAllSessions_AndSetsTokensRevocadosAntesDe()
        {
            // Arrange
            using var context = CreateDbContext();
            var service = CreateService(context);

            var usuario = new Usuario
            {
                Id = 19,
                Nombre = "User Masivo",
                Email = "masivo@test.com",
                PasswordHash = "h",
                Rol = "Cliente",
                Activo = true
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            await service.GenerarRefreshTokenAsync(usuario.Id, "1.1.1.1");
            await service.GenerarRefreshTokenAsync(usuario.Id, "2.2.2.2");
            await service.GenerarRefreshTokenAsync(usuario.Id, "3.3.3.3");

            // Act
            await service.RevocarTodosPorUsuarioAsync(usuario.Id, "Cambio de contraseña", "1.1.1.1");

            // Assert
            var tokens = await context.RefreshTokens.Where(r => r.UsuarioId == usuario.Id).ToListAsync();
            Assert.Equal(3, tokens.Count);
            Assert.All(tokens, t =>
            {
                Assert.NotNull(t.RevokedAt);
                Assert.Equal("Cambio de contraseña", t.ReasonRevoked);
            });

            var userInDb = await context.Usuarios.FindAsync(usuario.Id);
            Assert.NotNull(userInDb);
            Assert.NotNull(userInDb.TokensRevocadosAntesDe);
        }

        [Fact]
        public void ExtraerRefreshToken_PrioritizesCookie_ThenBody_ThenHeader()
        {
            // Arrange
            using var context = CreateDbContext();
            var service = CreateService(context);

            // Caso 1: Viene en cookie
            var httpContext1 = new DefaultHttpContext();
            httpContext1.Request.Headers["Cookie"] = "refreshToken=cookie-token-val";
            var token1 = service.ExtraerRefreshToken(httpContext1.Request, "body-token-val");
            Assert.Equal("cookie-token-val", token1);

            // Caso 2: No viene en cookie, viene en body
            var httpContext2 = new DefaultHttpContext();
            var token2 = service.ExtraerRefreshToken(httpContext2.Request, "body-token-val");
            Assert.Equal("body-token-val", token2);

            // Caso 3: No viene en cookie ni body, viene en header personalizado X-Refresh-Token
            var httpContext3 = new DefaultHttpContext();
            httpContext3.Request.Headers["X-Refresh-Token"] = "header-token-val";
            var token3 = service.ExtraerRefreshToken(httpContext3.Request, null);
            Assert.Equal("header-token-val", token3);

            // Caso 4: No viene en ningún lado
            var httpContext4 = new DefaultHttpContext();
            var token4 = service.ExtraerRefreshToken(httpContext4.Request, null);
            Assert.Null(token4);
        }
    }
}
