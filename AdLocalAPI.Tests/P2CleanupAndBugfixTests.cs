using System.Security.Claims;
using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Interfaces.Location;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Interfaces.Repository;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Interfaces.TipoComercio;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using Amazon.S3;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AdLocalAPI.Tests;

public class P2CleanupAndBugfixTests
{
    private static AppDbContext CrearDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task UsuarioRepository_DeleteAsync_EliminaUsuarioConClaveLongSinArgumentException()
    {
        await using var db = CrearDb();
        var usuario = new Usuario
        {
            Id = 17,
            Nombre = "Comercio Demo",
            Email = "demo@adlocal.store",
            PasswordHash = "hash",
            Rol = "Comercio",
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var repo = new UsuarioRepository(
            db,
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<IAmazonS3>());

        await repo.DeleteAsync(17);

        Assert.Null(await db.Usuarios.FindAsync(17L));
    }

    [Fact]
    public async Task ComercioService_GetComercioById_FiltraProductosActivosAntesDeAplicarMaxProductos()
    {
        var comercio = new Comercio
        {
            Id = 5,
            IdUsuario = 9,
            Nombre = "Café Centro",
            Activo = true
        };
        var propietario = new Usuario
        {
            Id = 9,
            Nombre = "Dueño",
            Email = "dueno@adlocal.store",
            PasswordHash = "hash",
            Rol = "Comercio",
            Activo = true
        };
        var suscripcion = new Suscripcion
        {
            Id = 1,
            UsuarioId = 9,
            PlanId = 2,
            StripeCustomerId = "cus_test",
            StripeSubscriptionId = "sub_test",
            StripePriceId = "price_basic",
            IsActive = true
        };
        var plan = new Plan
        {
            Id = 2,
            Nombre = "Básico",
            StripePriceId = "price_basic",
            Precio = 99m,
            DuracionDias = 30,
            Tipo = "BASIC",
            MaxNegocios = 1,
            MaxFotos = 5,
            MaxProductos = 2,
            PermiteCatalogo = true
        };

        var catalogo = new List<ProductosServicios>
        {
            new() { Id = 1, IdComercio = 5, IdUsuario = 9, Nombre = "Inactivo 1", Activo = false },
            new() { Id = 2, IdComercio = 5, IdUsuario = 9, Nombre = "Inactivo 2", Activo = false },
            new() { Id = 3, IdComercio = 5, IdUsuario = 9, Nombre = "Espresso", Activo = true },
            new() { Id = 4, IdComercio = 5, IdUsuario = 9, Nombre = "Capuchino", Activo = true },
            new() { Id = 5, IdComercio = 5, IdUsuario = 9, Nombre = "Latte", Activo = true }
        };

        var comercioRepo = new Mock<IComercioRepository>();
        comercioRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(comercio);

        var usuarioRepo = new Mock<IUsuarioRepository>();
        usuarioRepo.Setup(r => r.GetByIdComercioAsync(5)).ReturnsAsync(propietario);

        var susRepo = new Mock<ISuscripcionRepository>();
        susRepo.Setup(r => r.GetActivaByUsuarioAsync(9)).ReturnsAsync(suscripcion);
        susRepo.Setup(r => r.ObtenerBadgeTextoUsuarioAsync(9)).ReturnsAsync("");

        var planRepo = new Mock<IPlanRepository>();
        planRepo.Setup(r => r.GetByIdLongAsync(2)).ReturnsAsync(plan);

        var imgRepo = new Mock<IRelComercioImagenRepositorio>();
        imgRepo.Setup(r => r.ObtenerPorComercio(5)).ReturnsAsync([]);

        var horarioRepo = new Mock<IHorarioComercioRepository>();
        horarioRepo.Setup(r => r.ObtenerHorariosPorComercioAsync(5)).ReturnsAsync([]);

        var prodRepo = new Mock<IProductosServiciosRepository>();
        prodRepo.Setup(r => r.GetAllAsync(5)).ReturnsAsync(catalogo);

        var califRepo = new Mock<ICalificacionComentarioRepository>();
        califRepo.Setup(r => r.GetCalificacionByComercioAsync(5)).ReturnsAsync([]);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var service = new ComercioService(
            comercioRepo.Object,
            new JwtContext(new HttpContextAccessor { HttpContext = httpContext }),
            imgRepo.Object,
            horarioRepo.Object,
            Mock.Of<ITipoComercioRepository>(),
            susRepo.Object,
            Mock.Of<IGeoLocationService>(),
            planRepo.Object,
            usuarioRepo.Object,
            prodRepo.Object,
            Mock.Of<ILocationRepository>(),
            califRepo.Object,
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<IEmailService>(),
            Mock.Of<IRefreshTokenService>());

        var resp = await service.GetComercioById(5);

        Assert.True(resp.EsExitoso);
        Assert.NotNull(resp.Respuesta);
        Assert.NotNull(resp.Respuesta!.Productos);
        Assert.Equal(2, resp.Respuesta.Productos!.Count);
        Assert.All(resp.Respuesta.Productos, p => Assert.True(p.Activo));
        Assert.Equal(["Espresso", "Capuchino"], resp.Respuesta.Productos.Select(p => p.Nombre).ToArray());
    }

    [Fact]
    public void Dockerfile_IncluyeHealthcheckSobrePing()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Dockerfile")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var dockerfile = File.ReadAllText(Path.Combine(dir!.FullName, "Dockerfile"));
        Assert.Contains("HEALTHCHECK", dockerfile);
        Assert.Contains("/ping", dockerfile);
    }
}
