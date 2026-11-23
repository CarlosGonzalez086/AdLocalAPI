using System.Reflection;
using System.Security.Claims;
using AdLocalAPI.Constants;
using AdLocalAPI.Controllers;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Interfaces.Repository;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using Amazon.S3;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Moq;
using static AdLocalAPI.DTOs.ComercioVisitaDTOs;

namespace AdLocalAPI.Tests;

public class P0SecurityHardeningTests
{
    private static JwtContext CrearJwtContext(long? userId, string? rol, long comercioId = 0, int maxProductos = 10)
    {
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue && !string.IsNullOrWhiteSpace(rol))
        {
            var claims = new List<Claim>
            {
                new("id", userId.Value.ToString()),
                new("rol", rol),
                new(ClaimTypes.Role, rol),
                new("comercioId", comercioId.ToString()),
                new("maxProductos", maxProductos.ToString())
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        return new JwtContext(new HttpContextAccessor { HttpContext = httpContext });
    }

    private static AppDbContext CrearInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void ControladoresCriticos_TienenAtributosDeAutorizacionYRateLimitingP0()
    {
        var getAllSusc = typeof(SuscripcionesController).GetMethod(nameof(SuscripcionesController.GetAll))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(getAllSusc);
        Assert.Equal("Admin", getAllSusc!.Roles);

        var statsSusc = typeof(SuscripcionesController).GetMethod(nameof(SuscripcionesController.SuscripcionesStats))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(statsSusc);
        Assert.Equal("Admin", statsSusc!.Roles);

        var cuentasAttr = typeof(CuentasBancariasComercioController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(cuentasAttr);
        Assert.Equal("Comercio", cuentasAttr!.Roles);

        var pagosAttr = typeof(ConfiguracionPagoComercioController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(pagosAttr);
        Assert.Equal("Comercio", pagosAttr!.Roles);

        var prodAttr = typeof(ProductosServiciosController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(prodAttr);
        Assert.Equal("Comercio,Colaborador", prodAttr!.Roles);

        var califCrear = typeof(CalificacionesComentariosController).GetMethod(nameof(CalificacionesComentariosController.Crear))!;
        Assert.NotNull(califCrear.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("auth", califCrear.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);

        var usuarioCrear = typeof(UsuarioController).GetMethod(nameof(UsuarioController.CrearUsuario))!;
        Assert.Equal("auth", usuarioCrear.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);

        var contarRef = typeof(UsoCodigoReferidoController).GetMethod(nameof(UsoCodigoReferidoController.ContarPorCodigo))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal("Admin", contarRef?.Roles);

        var totalRef = typeof(UsoCodigoReferidoController).GetMethod(nameof(UsoCodigoReferidoController.TotalUsos))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal("Admin", totalRef?.Roles);

        var comerciosCrear = typeof(ComerciosController).GetMethod(nameof(ComerciosController.Crear))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal("Comercio", comerciosCrear?.Roles);

        var comerciosEliminar = typeof(ComerciosController).GetMethod(nameof(ComerciosController.Eliminar))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal("Comercio", comerciosEliminar?.Roles);
    }

    [Fact]
    public async Task ProductosServicios_CreateAsync_RechazaComercioAjenoCon403()
    {
        using var db = CrearInMemoryDb();
        var prodRepo = new Mock<IProductosServiciosRepository>();
        var comercioRepo = new Mock<IComercioRepository>();
        comercioRepo.Setup(r => r.GetByIdAsync(99))
            .ReturnsAsync(new Comercio { Id = 99, IdUsuario = 999, Activo = true, Visible = true });

        var validator = new Mock<IValidator<ProductosServiciosDto>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ProductosServiciosDto>(), default))
            .ReturnsAsync(new ValidationResult());

        var service = new ProductosServiciosService(
            prodRepo.Object,
            comercioRepo.Object,
            CrearJwtContext(userId: 10, rol: "Comercio", comercioId: 10),
            new SuscripcionRepository(db),
            new UsuarioRepository(db, new Mock<IWebHostEnvironment>().Object, new Mock<IAmazonS3>().Object),
            validator.Object);

        var resp = await service.CreateAsync(new ProductosServiciosDto
        {
            IdComercio = 99,
            Nombre = "Inyección",
            Tipo = 1,
            Modalidad = 1,
            Precio = 50
        });

        Assert.Equal("403", resp.Codigo);
        prodRepo.Verify(r => r.CreateAsync(It.IsAny<ProductosServicios>()), Times.Never);
    }

    [Fact]
    public async Task Beneficios_ReclamarBeneficio_RechazaSegundoReclamoYValidaStripeCustomer()
    {
        var userRepo = new Mock<IUsuarioRepository>();
        var suscRepo = new Mock<ISuscripcionRepository>();
        var planRepo = new Mock<IPlanRepository>();
        var refRepo = new Mock<IUsoCodigoReferidoRepository>();

        userRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Usuario
        {
            Id = 10,
            CodigoReferido = "REF10",
            RedeemRewards = true
        });

        var service = new BeneficiosServices(
            CrearJwtContext(10, "Comercio"),
            userRepo.Object,
            suscRepo.Object,
            refRepo.Object,
            planRepo.Object);

        var yaReclamado = await service.ReclamarBeneficio();
        Assert.Equal("400", yaReclamado.Codigo);
        Assert.Contains("ya fue reclamado", yaReclamado.Mensaje, StringComparison.OrdinalIgnoreCase);

        userRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(new Usuario
        {
            Id = 11,
            CodigoReferido = "REF11",
            RedeemRewards = false,
            StripeCustomerId = ""
        });
        refRepo.Setup(r => r.ContarPorCodigoAsync("REF11")).ReturnsAsync(10);
        suscRepo.Setup(r => r.GetActivaByUsuarioAsync(11)).ReturnsAsync((Suscripcion?)null);

        var sinStripe = await new BeneficiosServices(
            CrearJwtContext(11, "Comercio"),
            userRepo.Object,
            suscRepo.Object,
            refRepo.Object,
            planRepo.Object).ReclamarBeneficio();

        Assert.Equal("400", sinStripe.Codigo);
        Assert.Contains("Stripe", sinStripe.Mensaje);
    }

    [Fact]
    public async Task ComercioVisitas_GetStats_RechazaUsuarioAjenoYAutorizaDueno()
    {
        var visitasRepo = new Mock<IComercioVisitaRepository>();
        visitasRepo.Setup(r => r.GetStats(50)).ReturnsAsync(new ComercioVisitasStatsDto());
        var comercioRepo = new Mock<IComercioRepository>();
        comercioRepo.Setup(r => r.GetByIdAsync(50))
            .ReturnsAsync(new Comercio { Id = 50, IdUsuario = 5, Activo = true });

        var ajeno = new ComercioVisitaService(visitasRepo.Object, comercioRepo.Object, CrearJwtContext(99, "Comercio"));
        Assert.Equal("403", (await ajeno.GetStats(50)).Codigo);

        var cliente = new ComercioVisitaService(visitasRepo.Object, comercioRepo.Object, CrearJwtContext(5, "Cliente"));
        Assert.Equal("403", (await cliente.GetStats(50)).Codigo);

        var dueno = new ComercioVisitaService(visitasRepo.Object, comercioRepo.Object, CrearJwtContext(5, "Comercio"));
        Assert.Equal("200", (await dueno.GetStats(50)).Codigo);
    }

    [Fact]
    public async Task Calificaciones_CrearComentario_RechazaAnonimoYComercioInexistente()
    {
        var califRepo = new Mock<ICalificacionComentarioRepository>();
        var comercioRepo = new Mock<IComercioRepository>();

        var anonimo = new CalificacionComentarioService(
            califRepo.Object,
            comercioRepo.Object,
            CrearJwtContext(null, null));

        var respAnon = await anonimo.CrearComentario(new CalificacionComentarioCreateDto
        {
            IdComercio = 1,
            Calificacion = 5,
            Comentario = "Excelente",
            NombrePersona = "Anon"
        });
        Assert.Equal("401", respAnon.Codigo);

        comercioRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Comercio?)null);
        var autenticado = new CalificacionComentarioService(
            califRepo.Object,
            comercioRepo.Object,
            CrearJwtContext(12, "Cliente"));

        var resp404 = await autenticado.CrearComentario(new CalificacionComentarioCreateDto
        {
            IdComercio = 404,
            Calificacion = 5,
            Comentario = "Excelente",
            NombrePersona = "Cliente"
        });
        Assert.Equal("404", resp404.Codigo);
        califRepo.Verify(r => r.CreateAsync(It.IsAny<CalificacionComentario>()), Times.Never);
    }

    [Fact]
    public async Task Configuracion_EnmascaraSecretosYNoSobrescribeConMascara()
    {
        var store = new List<ConfiguracionSistema>
        {
            new() { Id = 1, Key = ConfiguracionKeys.StripeSecretKey, Val = "sk_test_1234567890ABCD" },
            new() { Id = 2, Key = ConfiguracionKeys.EmailKey, Val = "xkeysib-secret-9999" },
            new() { Id = 3, Key = ConfiguracionKeys.Ip2LocationKey, Val = "ip2loc-key-WXYZ" },
            new() { Id = 4, Key = ConfiguracionKeys.StripePublishableKey, Val = "pk_test_publico" }
        };

        var repo = new Mock<IConfiguracionRepository>();
        repo.Setup(r => r.ObtenerTodosAsync()).ReturnsAsync(store);
        repo.Setup(r => r.ObtenerPorKeyAsync(It.IsAny<string>()))
            .ReturnsAsync((string k) => store.FirstOrDefault(x => x.Key == k));
        repo.Setup(r => r.ActualizarAsync(It.IsAny<ConfiguracionSistema>()))
            .ReturnsAsync((ConfiguracionSistema c) => c);

        var service = new ConfiguracionService(repo.Object, new Mock<IEmailService>().Object);

        var listado = await service.ObtenerTodosAsync();
        Assert.Equal("200", listado.Codigo);
        Assert.Equal("••••ABCD", listado.Respuesta!.Single(x => x.Key == ConfiguracionKeys.StripeSecretKey).Val);
        Assert.Equal("••••9999", listado.Respuesta!.Single(x => x.Key == ConfiguracionKeys.EmailKey).Val);
        Assert.Equal("••••WXYZ", listado.Respuesta!.Single(x => x.Key == ConfiguracionKeys.Ip2LocationKey).Val);
        Assert.Equal("pk_test_publico", listado.Respuesta!.Single(x => x.Key == ConfiguracionKeys.StripePublishableKey).Val);

        var guardarMascara = await service.CrearOActualizarAsync(new ConfiguracionSistemaDto
        {
            Key = ConfiguracionKeys.StripeSecretKey,
            Val = "••••ABCD"
        });
        Assert.Equal("200", guardarMascara.Codigo);
        Assert.Equal("sk_test_1234567890ABCD", store.Single(x => x.Key == ConfiguracionKeys.StripeSecretKey).Val);
        repo.Verify(r => r.ActualizarAsync(It.IsAny<ConfiguracionSistema>()), Times.Never);
    }
}
