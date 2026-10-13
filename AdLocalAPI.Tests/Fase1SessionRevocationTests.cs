using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AdLocalAPI.Tests;

public class Fase1SessionRevocationTests
{
    [Fact]
    public async Task RevocarTodas_ConNoTracking_PersisteRevocacionYCorteDeJwtSinAfectarOtraCuenta()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options;
        var before = DateTime.UtcNow;
        await using (var seed = new AppDbContext(options)) {
            seed.Usuarios.AddRange(new Usuario { Id = 7, Nombre = "Prueba", Email = "test7@example.test", Rol = "Comercio", PasswordHash = "synthetic" },
                new Usuario { Id = 8, Nombre = "Otra", Email = "test8@example.test", Rol = "Comercio", PasswordHash = "synthetic" });
            seed.RefreshTokens.AddRange(new RefreshToken { UsuarioId = 7, TokenHash = "synthetic-a", ExpiresAt = before.AddDays(1) },
                new RefreshToken { UsuarioId = 7, TokenHash = "synthetic-b", ExpiresAt = before.AddDays(1) },
                new RefreshToken { UsuarioId = 8, TokenHash = "synthetic-c", ExpiresAt = before.AddDays(1) });
            await seed.SaveChangesAsync();
        }
        await using (var operation = new AppDbContext(options)) {
            var service = new RefreshTokenService(new RefreshTokenRepository(operation), NullLogger<RefreshTokenService>.Instance);
            await service.RevocarTodosPorUsuarioAsync(7, "Prueba de revocación", "127.0.0.1");
        }
        // Contexto nuevo: impide confundir un objeto modificado en memoria con persistencia real.
        await using var observer = new AppDbContext(options);
        var tokens = await observer.RefreshTokens.Where(t => t.UsuarioId == 7).ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.All(tokens, t => { Assert.NotNull(t.RevokedAt); Assert.False(t.IsActive); Assert.Equal("Prueba de revocación", t.ReasonRevoked); });
        var user = await observer.Usuarios.SingleAsync(u => u.Id == 7);
        Assert.NotNull(user.TokensRevocadosAntesDe);
        var identity = new ClaimsIdentity(new[] { new Claim("id", "7"), new Claim("rol", "Comercio"), new Claim(ClaimTypes.Role, "Comercio"),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(before.AddMinutes(-5)).ToUnixTimeSeconds().ToString()) }, "Test");
        Assert.False(TokenSecurityValidator.ValidarUsuarioYClaims(user, new ClaimsPrincipal(identity)).IsValid);
        Assert.True((await observer.RefreshTokens.SingleAsync(t => t.UsuarioId == 8)).IsActive);
        Assert.Null((await observer.Usuarios.SingleAsync(u => u.Id == 8)).TokensRevocadosAntesDe);
    }
}
