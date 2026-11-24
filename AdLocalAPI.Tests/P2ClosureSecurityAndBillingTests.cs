using System.Reflection;
using System.Security.Claims;
using System.Text;
using AdLocalAPI.Controllers;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Middlewares;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.Repository;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AdLocalAPI.Tests;

public class P2ClosureSecurityAndBillingTests
{
    private static UsuarioService CrearUsuarioService(IUsuarioRepository repo, ISuscripcionRepository? susRepo = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "clave-super-secreta-para-pruebas-unitarias-32bytes!",
                ["Jwt:Issuer"] = "AdLocalAPI",
                ["Jwt:Audience"] = "AdLocal"
            })
            .Build();

        return new UsuarioService(
            repo,
            config,
            new JwtContext(new HttpContextAccessor { HttpContext = new DefaultHttpContext() }),
            Mock.Of<IComercioRepository>(),
            Mock.Of<IEmailService>(),
            Mock.Of<IWebHostEnvironment>(),
            susRepo ?? Mock.Of<ISuscripcionRepository>(),
            Mock.Of<IPlanRepository>(),
            Mock.Of<IUsoCodigoReferidoRepository>(),
            Mock.Of<IRefreshTokenService>(),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            NullLogger<UsuarioService>.Instance);
    }

    [Fact]
    public async Task NewPassword_RequiereTokenOCorreoYBloqueaIntentosFallidos()
    {
        var usuario = new Usuario
        {
            Id = 10,
            Email = "owner@adlocal.store",
            Nombre = "Owner",
            Rol = "Comercio",
            Codigo = "ABC123",
            Token = "tok-real-123",
            CodigoExpiracion = DateTime.UtcNow.AddMinutes(10),
            IntentosCodigo = 0,
            PasswordHash = "oldhash"
        };

        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.GetByTokenAsync("tok-real-123")).ReturnsAsync(usuario);
        repo.Setup(r => r.GetByCorreoAsync("owner@adlocal.store")).ReturnsAsync(usuario);
        repo.Setup(r => r.GetByCodeAsync("ABC123")).ReturnsAsync(usuario);

        var service = CrearUsuarioService(repo.Object);

        // Sin Token ni Email no permite buscar por codigo global
        var sinIdentidad = await service.NewPassword(new NewPasswordDto
        {
            Codigo = "ABC123",
            PasswordNueva = "ClaveSegura2026!"
        });
        Assert.Equal("400", sinIdentidad.Codigo);
        repo.Verify(r => r.GetByCodeAsync(It.IsAny<string>()), Times.Never);

        // Codigo erroneo incrementa IntentosCodigo y bloquea al 5to intento
        for (int intento = 1; intento <= 5; intento++)
        {
            var res = await service.NewPassword(new NewPasswordDto
            {
                Token = "tok-real-123",
                Codigo = "WRONG1",
                PasswordNueva = "ClaveSegura2026!"
            });
            Assert.Equal("400", res.Codigo);
        }

        Assert.Null(usuario.Codigo);
        Assert.Null(usuario.Token);
        Assert.Null(usuario.CodigoExpiracion);
        Assert.Equal(0, usuario.IntentosCodigo);
    }

    [Fact]
    public async Task NewPassword_Y_CheckToken_RechazanCodigoExpiracionNull()
    {
        var usuario = new Usuario
        {
            Id = 11,
            Email = "colab@adlocal.store",
            Nombre = "Colab",
            Rol = "Colaborador",
            Codigo = "COL123",
            Token = "tok-colab",
            CodigoExpiracion = null,
            PasswordHash = ""
        };

        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.GetByTokenAsync("tok-colab")).ReturnsAsync(usuario);

        var service = CrearUsuarioService(repo.Object);

        var check = await service.CheckToken("tok-colab");
        Assert.Equal("400", check.Codigo);

        usuario.Codigo = "COL123";
        usuario.Token = "tok-colab";
        var reset = await service.NewPassword(new NewPasswordDto
        {
            Token = "tok-colab",
            Codigo = "COL123",
            PasswordNueva = "ClaveSegura2026!"
        });
        Assert.Equal("400", reset.Codigo);
    }

    [Fact]
    public async Task Login_ColaboradorSinPasswordConfigurada_Retorna401SinExcepcionBCrypt()
    {
        var colab = new Usuario
        {
            Id = 20,
            Email = "nuevo@adlocal.store",
            Rol = "Colaborador",
            Activo = true,
            ComercioId = 5,
            PasswordHash = ""
        };
        var owner = new Usuario { Id = 21, Rol = "Comercio", Activo = true };
        var sub = new Suscripcion
        {
            Id = 1,
            UsuarioId = 21,
            Plan = new Plan { Id = 3, Tipo = "PRO", IsMultiUsuario = true, StripePriceId = "p" },
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(10)
        };

        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.GetByCorreoAsync("nuevo@adlocal.store")).ReturnsAsync(colab);
        repo.Setup(r => r.GetByIdComercioAsync(5)).ReturnsAsync(owner);

        var susRepo = new Mock<ISuscripcionRepository>();
        susRepo.Setup(r => r.GetActivaByUsuarioAsync(21)).ReturnsAsync(sub);

        var service = CrearUsuarioService(repo.Object, susRepo.Object);
        var res = await service.Login("nuevo@adlocal.store", "Cualquiera123");

        Assert.Equal("401", res.Codigo);
    }

    [Fact]
    public void MediaBase64Validator_ValidaMagicBytesYTamanoMaximo()
    {
        var validPng = Convert.ToBase64String([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]);
        Assert.True(MediaBase64Validator.TryDecodeValidatedImage(
            $"data:image/png;base64,{validPng}",
            out var bytes,
            out var ct,
            out _));
        Assert.Equal("image/png", ct);
        Assert.NotEmpty(bytes);

        var spoofedHtml = Convert.ToBase64String(Encoding.UTF8.GetBytes("<script>alert(1)</script>"));
        Assert.False(MediaBase64Validator.TryDecodeValidatedImage(
            $"data:image/png;base64,{spoofedHtml}",
            out _,
            out _,
            out _));

        Assert.False(MediaBase64Validator.TryDecodeValidatedImage(
            spoofedHtml,
            out _,
            out _,
            out _));
    }

    [Fact]
    public void UrlHelper_NoGeneraDoblesDiagonalesEnProduccion()
    {
        var passUrl = UrlHelper.GenerarLinkCambioPassword("abc-123", true, "user");
        var colabUrl = UrlHelper.GenerarLinkNuevoColaborador("xyz-999", true);

        Assert.Equal("https://panel.adlocal.store/usuario/restablecer-contrasena/abc-123/user", passUrl);
        Assert.Equal("https://panel.adlocal.store/usuario/nuevo-colaborador/xyz-999", colabUrl);
    }

    [Fact]
    public void ControladoresCriticos_ExigenRolComercioORateLimiting()
    {
        var checkoutAuth = typeof(global::CheckoutController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(checkoutAuth);
        Assert.Equal("Comercio", checkoutAuth!.Roles);

        var benefAuth = typeof(BeneficiosController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(benefAuth);
        Assert.Equal("Comercio", benefAuth!.Roles);

        var registrar = typeof(ComercioVisitasController).GetMethod(nameof(ComercioVisitasController.Registrar));
        Assert.NotNull(registrar);
        var rateAttr = registrar!.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(rateAttr);
        Assert.Equal("auth", rateAttr!.PolicyName);
    }

    [Fact]
    public async Task WebhookService_OnInvoicePaymentSucceeded_DesactivaSuscripcionPreviaCuandoExistePending()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);

        var planFree = new Plan { Id = 1, Nombre = "Free", Tipo = "FREE", StripePriceId = "", Precio = 0, DuracionDias = 30 };
        var planPro = new Plan { Id = 2, Nombre = "Pro", Tipo = "PRO", StripePriceId = "price_pro", Precio = 199, DuracionDias = 30 };
        var user = new Usuario { Id = 77, Nombre = "Tienda", Email = "t@adlocal.store", PasswordHash = "h", Rol = "Comercio", StripeCustomerId = "cus_77" };
        db.Plans.AddRange(planFree, planPro);
        db.Usuarios.Add(user);
        db.Suscripcions.AddRange(
            new Suscripcion
            {
                Id = 1,
                UsuarioId = 77,
                PlanId = 1,
                StripeCustomerId = "cus_77",
                StripeSubscriptionId = "",
                StripePriceId = "",
                Status = "active",
                IsActive = true
            },
            new Suscripcion
            {
                Id = 2,
                UsuarioId = 77,
                PlanId = 2,
                StripeCustomerId = "cus_77",
                StripeSubscriptionId = "sub_paid_77",
                StripePriceId = "",
                Status = "pending",
                IsActive = false
            });
        await db.SaveChangesAsync();

        var susRepo = new SuscripcionRepository(db);
        var webhookService = new WebhookService(
            Mock.Of<IStripeWebhookEventRepository>(),
            susRepo,
            Mock.Of<IUsuarioRepository>(),
            Mock.Of<IPlanRepository>(),
            new ConfigurationBuilder().Build(),
            NullLogger<WebhookService>.Instance);

        var desactivar = typeof(WebhookService).GetMethod(
            "DesactivarSuscripcionesPreviasAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(desactivar);

        await (Task)desactivar!.Invoke(webhookService, [77L, 2])!;

        var freeActualizada = await db.Suscripcions.FindAsync(1);
        Assert.NotNull(freeActualizada);
        Assert.False(freeActualizada!.IsActive);
        Assert.Equal("canceled", freeActualizada.Status);
    }

    [Fact]
    public async Task SuscripcionService_CancelarPlanFree_Retorna400SinLlamarStripe()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("id", "99"), new Claim("rol", "Comercio")]))
        };
        var susRepo = new Mock<ISuscripcionRepository>();
        susRepo.Setup(r => r.GetActivaByUsuarioAsync(99)).ReturnsAsync(new Suscripcion
        {
            Id = 5,
            UsuarioId = 99,
            PlanId = 1,
            StripeSubscriptionId = "",
            Status = "active",
            IsActive = true
        });

        var service = new SuscripcionService(
            new JwtContext(new HttpContextAccessor { HttpContext = httpContext }),
            Mock.Of<IUsuarioRepository>(),
            susRepo.Object,
            Mock.Of<IPlanRepository>(),
            Mock.Of<IWebHostEnvironment>());

        var res = await service.CancelarPlan();
        Assert.Equal("400", res.Codigo);
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_SanitizaErrorInterno500EnProduccion()
    {
        var prodEnv = new Mock<IWebHostEnvironment>();
        prodEnv.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

        var middleware = new GlobalExceptionMiddleware(
            _ => throw new Exception("DetalleSQLPrivado_TablaX"),
            NullLogger<GlobalExceptionMiddleware>.Instance,
            prodEnv.Object);

        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(ctx);

        ctx.Response.Body.Position = 0;
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        Assert.Equal(500, ctx.Response.StatusCode);
        Assert.DoesNotContain("DetalleSQLPrivado_TablaX", body);
    }

    [Fact]
    public async Task RefreshTokenRepository_ObtenerPorHashConUsuario_HabilitaTrackingParaRotacion()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);

        var user = new Usuario { Id = 88, Nombre = "User", Email = "u88@adlocal.store", PasswordHash = "h", Rol = "Cliente" };
        db.Usuarios.Add(user);
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = 1,
            UsuarioId = 88,
            TokenHash = "hash-88",
            ExpiresAt = DateTime.UtcNow.AddDays(10)
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new RefreshTokenRepository(db, new MemoryCache(new MemoryCacheOptions()));
        var loaded = await repo.ObtenerPorHashConUsuarioAsync("hash-88");

        Assert.NotNull(loaded);
        Assert.NotEmpty(db.ChangeTracker.Entries<RefreshToken>());
    }

    [Fact]
    public void ProgramCs_ExigeWebhookSecretEnProduccionYForwardLimitUno()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AdLocalAPI", "Program.cs")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var program = File.ReadAllText(Path.Combine(dir!.FullName, "AdLocalAPI", "Program.cs"));
        Assert.DoesNotContain("//if (builder.Environment.IsProduction())", program);
        Assert.Contains("ForwardLimit = 1", program);
    }

    [Fact]
    public void StripeYFacturacionControllers_RestringenRolComercio()
    {
        var stripeAttr = typeof(StripeController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(stripeAttr);
        Assert.Equal("Comercio", stripeAttr!.Roles);

        var tarjetasAttr = typeof(TarjetasController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(tarjetasAttr);
        Assert.Equal("Comercio", tarjetasAttr!.Roles);

        var miSusc = typeof(SuscripcionesController).GetMethod(nameof(SuscripcionesController.MiSuscripcion))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(miSusc);
        Assert.Equal("Comercio", miSusc!.Roles);

        var reconciliar = typeof(SuscripcionesController).GetMethod(nameof(SuscripcionesController.ReconciliarMiSuscripcion))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(reconciliar);
        Assert.Equal("Comercio", reconciliar!.Roles);
    }

    [Fact]
    public async Task UsuarioService_ActualizarUsuario_RechazaCorreoDuplicadoYReseteaVerificacion()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new AppDbContext(dbOptions);
        var userRepo = new Mock<IUsuarioRepository>();
        var actual = new Usuario
        {
            Id = 55,
            Nombre = "Ana",
            Email = "ana@adlocal.store",
            EmailVerificado = true,
            Rol = "Cliente",
            Activo = true
        };
        userRepo.Setup(r => r.GetByIdAsync(55)).ReturnsAsync(actual);
        userRepo.Setup(r => r.ExistePorCorreoAsync("admin@adlocal.store")).ReturnsAsync(true);
        userRepo.Setup(r => r.ExistePorCorreoAsync("ana.nueva@adlocal.store")).ReturnsAsync(false);

        var rfService = new RefreshTokenService(
            new RefreshTokenRepository(db, new MemoryCache(new MemoryCacheOptions())),
            NullLogger<RefreshTokenService>.Instance);

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("id", "55"), new Claim("rol", "Cliente")], "Test"))
            }
        };

        var service = new UsuarioService(
            userRepo.Object,
            new ConfigurationBuilder().Build(),
            new JwtContext(accessor),
            Mock.Of<IComercioRepository>(),
            Mock.Of<IEmailService>(),
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<ISuscripcionRepository>(),
            Mock.Of<IPlanRepository>(),
            Mock.Of<IUsoCodigoReferidoRepository>(),
            rfService,
            accessor);

        var dup = await service.ActualizarUsuario(new UsuarioUpdateDto { Nombre = "Ana", Email = "admin@adlocal.store" });
        Assert.Equal("409", dup.Codigo);
        Assert.True(actual.EmailVerificado);

        var ok = await service.ActualizarUsuario(new UsuarioUpdateDto { Nombre = "Ana Nueva", Email = "ANA.NUEVA@adlocal.store" });
        Assert.Equal("200", ok.Codigo);
        Assert.Equal("ana.nueva@adlocal.store", actual.Email);
        Assert.False(actual.EmailVerificado);
    }
}
