using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdLocalAPI.Tests;

public class Fase2ArchitectureOptimizationTests
{
    [Fact]
    public async Task ValidarContextoAsync_CachesUserInIMemoryCache_AvoidingDuplicateDbQueries()
    {
        // Arrange
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddMemoryCache();

        var sp = services.BuildServiceProvider();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Usuarios.Add(new Usuario
            {
                Id = 42,
                Nombre = "Usuario Cache Test",
                Email = "cache@test.com",
                Rol = "Cliente",
                Activo = true,
                PasswordHash = "fakehash"
            });
            await db.SaveChangesAsync();
        }

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var claims = new[]
        {
            new Claim("id", "42"),
            new Claim(ClaimTypes.Role, "Cliente"),
            new Claim("rol", "Cliente"),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new TokenValidatedContext(httpContext, new Microsoft.AspNetCore.Authentication.AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), new JwtBearerOptions())
        {
            Principal = principal
        };

        var cache = sp.GetRequiredService<IMemoryCache>();
        var cacheKey = TokenSecurityValidator.ObtenerClaveCache(42);

        // Precondition: cache is empty
        Assert.False(cache.TryGetValue(cacheKey, out Usuario? _));

        // Act 1: First validation -> fetches from DB and populates cache
        await TokenSecurityValidator.ValidarContextoAsync(context);

        // Assert 1: Token should be valid and user should now be in cache
        Assert.False(context.Result?.Failure != null);
        Assert.True(cache.TryGetValue(cacheKey, out Usuario? cachedUser));
        Assert.NotNull(cachedUser);
        Assert.Equal(42, cachedUser!.Id);
        Assert.Equal("Cliente", cachedUser.Rol);

        // Act 2: Modify the DB record directly. Because cache is active with 60s TTL,
        // subsequent validation should use the cached entity without hitting DB
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userInDb = await db.Usuarios.FindAsync(42L);
            userInDb!.Activo = false; // deactivate in DB
            await db.SaveChangesAsync();
        }

        var context2 = new TokenValidatedContext(httpContext, new Microsoft.AspNetCore.Authentication.AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), new JwtBearerOptions())
        {
            Principal = principal
        };

        await TokenSecurityValidator.ValidarContextoAsync(context2);

        // Assert 2: Cached user was still active in cache, so context2 does not fail immediately from DB
        Assert.False(context2.Result?.Failure != null);

        // Act 3: Explicitly invalidate cache
        TokenSecurityValidator.InvalidarCacheUsuario(cache, 42);
        Assert.False(cache.TryGetValue(cacheKey, out Usuario? _));

        // Act 4: Third validation -> now hits DB, discovers user is inactive, fails and clears cache
        var context3 = new TokenValidatedContext(httpContext, new Microsoft.AspNetCore.Authentication.AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), new JwtBearerOptions())
        {
            Principal = principal
        };
        await TokenSecurityValidator.ValidarContextoAsync(context3);

        Assert.True(context3.Result?.Failure != null);
        Assert.Contains("inactivo", context3.Result?.Failure?.Message ?? "");
    }

    [Fact]
    public async Task RefreshTokenRepository_RevocarTodosPorUsuarioAsync_InvalidatesCachedUserSession()
    {
        // Arrange
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddMemoryCache();

        var sp = services.BuildServiceProvider();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Usuarios.Add(new Usuario
            {
                Id = 99,
                Nombre = "Revoke User",
                Email = "revoke@test.com",
                Rol = "Cliente",
                Activo = true,
                PasswordHash = "fakehash"
            });
            await db.SaveChangesAsync();
        }

        var cache = sp.GetRequiredService<IMemoryCache>();
        var cacheKey = TokenSecurityValidator.ObtenerClaveCache(99);

        // Populate cache
        cache.Set(cacheKey, new Usuario { Id = 99, Activo = true, Rol = "Cliente" }, TimeSpan.FromMinutes(5));
        Assert.True(cache.TryGetValue(cacheKey, out Usuario? _));

        // Act: Revoke all tokens for user 99
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repo = new RefreshTokenRepository(db, cache);
            await repo.RevocarTodosPorUsuarioAsync(99, "127.0.0.1", "Prueba de revocación");
        }

        // Assert: Cache must have been invalidated
        Assert.False(cache.TryGetValue(cacheKey, out Usuario? _));
    }
}
