using System.Security.Claims;
using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Repository;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static AdLocalAPI.DTOs.PagosComercio;

namespace AdLocalAPI.Tests;

public class P1OperationalStabilityTests
{
    private static AppDbContext CrearDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static JwtContext CrearJwt(long userId, string rol, long comercioId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new("id", userId.ToString()),
                new("rol", rol),
                new(ClaimTypes.Role, rol),
                new("comercioId", comercioId.ToString())
            }, "TestAuth"))
        };
        return new JwtContext(new HttpContextAccessor { HttpContext = httpContext });
    }

    [Fact]
    public async Task ComercioRepository_GetByFiltros_SoportaUbicacionNullYBusquedaPorTexto()
    {
        using var db = CrearDb();
        db.Estados.Add(new Estado { Id = 1, EstadoNombre = "Jalisco" });
        db.Municipios.Add(new Municipio { Id = 1, MunicipioNombre = "Guadalajara" });
        db.Comercios.AddRange(
            new Comercio
            {
                Id = 10,
                IdUsuario = 100,
                Nombre = "Café Artesanal Centro",
                Descripcion = "Tostado diario de especialidad",
                EstadoId = 1,
                MunicipioId = 1,
                Activo = true,
                Visible = true,
                Ubicacion = null, // No debe lanzar NullReferenceException
                FechaCreacion = DateTime.UtcNow.AddDays(-2)
            },
            new Comercio
            {
                Id = 11,
                IdUsuario = 101,
                Nombre = "Ferretería Norte",
                Descripcion = "Herramientas y tornillería",
                EstadoId = 1,
                MunicipioId = 1,
                Activo = true,
                Visible = true,
                Ubicacion = null,
                FechaCreacion = DateTime.UtcNow.AddDays(-1)
            });
        await db.SaveChangesAsync();

        var repo = new ComercioRepository(
            db,
            new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().Object,
            new Mock<Amazon.S3.IAmazonS3>().Object);

        var (items, total) = await repo.GetByFiltros(0, 0, 0, "alfabetico", 1, 8, "café");

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("Café Artesanal Centro", items[0].Nombre);
        Assert.Equal(0, items[0].Lat);
        Assert.Equal(0, items[0].Lng);
    }

    [Fact]
    public async Task PagosMultiComercio_PermiteSegundoComercioPropioYRechazaAjeno()
    {
        var comercioRepo = new Mock<IComercioRepository>();
        comercioRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Comercio { Id = 10, IdUsuario = 5, Activo = true });
        comercioRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(new Comercio { Id = 20, IdUsuario = 5, Activo = true });
        comercioRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(new Comercio { Id = 99, IdUsuario = 777, Activo = true });

        var pagoRepo = new Mock<IConfiguracionPagoComercioRepository>();
        pagoRepo.Setup(r => r.ObtenerPorComercioAsync(20)).ReturnsAsync(new ConfiguracionPagoComercio
        {
            Id = 1,
            IdComercio = 20,
            AceptaEfectivo = true,
            AceptaTransferencia = false,
            Activo = true
        });

        var jwt = CrearJwt(userId: 5, rol: "Comercio", comercioId: 10);
        var pagoService = new ConfiguracionPagoComercioService(pagoRepo.Object, comercioRepo.Object, jwt);

        var propioSegundo = await pagoService.Obtener(comercioId: 20);
        Assert.Equal("200", propioSegundo.Codigo);
        Assert.Equal(20, propioSegundo.Respuesta!.IdComercio);

        var ajeno = await pagoService.Obtener(comercioId: 99);
        Assert.Equal("403", ajeno.Codigo);

        var cuentaRepo = new Mock<ICuentaBancariaComercioRepository>();
        cuentaRepo.Setup(r => r.ObtenerTodasAsync(20)).ReturnsAsync(new List<CuentaBancariaComercio>());
        var cuentaService = new CuentaBancariaComercioService(cuentaRepo.Object, comercioRepo.Object, jwt);

        Assert.Equal("200", (await cuentaService.ObtenerTodas(comercioId: 20)).Codigo);
        Assert.Equal("403", (await cuentaService.ObtenerTodas(comercioId: 99)).Codigo);
    }

    [Fact]
    public async Task WebhookService_OnSubscriptionDeleted_NoDuplicaFreeSiYaExiste()
    {
        using var db = CrearDb();
        var planFree = new Plan { Id = 1, Nombre = "Free", Tipo = "FREE", Activo = true };
        var subRepo = new Mock<ISuscripcionRepository>();
        subRepo.Setup(r => r.ObtenerPorStripeId("sub_123")).ReturnsAsync((Suscripcion?)null);
        subRepo.Setup(r => r.GetActivaByUsuarioAsync(15)).ReturnsAsync(new Suscripcion
        {
            Id = 5,
            UsuarioId = 15,
            PlanId = 1,
            Plan = planFree,
            IsActive = true,
            Status = "active"
        });

        var userRepo = new Mock<IUsuarioRepository>();
        userRepo.Setup(r => r.GetByStripeId("cus_123")).ReturnsAsync(new Usuario { Id = 15, Email = "u@test.com" });

        var planRepo = new Mock<IPlanRepository>();
        planRepo.Setup(r => r.GetByTipoAsync("FREE")).ReturnsAsync(planFree);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Stripe:WebhookSecret"] = "whsec_test" })
            .Build();

        var service = new WebhookService(
            new StripeWebhookEventRepository(db),
            subRepo.Object,
            userRepo.Object,
            planRepo.Object,
            config,
            NullLogger<WebhookService>.Instance);

        var method = typeof(WebhookService).GetMethod(
            "OnSubscriptionDeleted",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        await (Task)method.Invoke(service, new object[]
        {
            new Stripe.Subscription { Id = "sub_123", CustomerId = "cus_123" }
        })!;

        subRepo.Verify(r => r.CrearAsync(It.IsAny<Suscripcion>()), Times.Never);
    }
}
