using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AdLocalAPI.Controllers;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Interfaces.Location;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Interfaces.TipoComercio;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace AdLocalAPI.Tests.Integration;

// HTTP real sobre Kestrel, con JWT firmado y los controladores/servicios de la API.
// Repositorios simulados y EF InMemory: no accede a proveedores ni a bases reales.
public sealed class Fase1SecurityHost : IAsyncLifetime
{
    public const string Key = "Phase1SyntheticSigningKeyForHttpSecurityTestsOnly!";
    private WebApplication _app = null!;
    public HttpClient Client { get; private set; } = null!;
    public Mock<IUsuarioRepository> Users { get; } = new();
    public Mock<IPlanRepository> Plans { get; } = new();
    public Mock<IComercioRepository> Comercios { get; } = new();
    public Mock<IRefreshTokenService> Refresh { get; } = new();
    public Dictionary<long, Usuario> Records { get; } = new();
    public IServiceProvider Services => _app.Services;

    public async Task InitializeAsync()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("SyntheticPassword123!");
        foreach (var (id, role) in new[] { (1L, "Cliente"), (2L, "Admin"), (3L, "Comercio"), (4L, "Colaborador"), (5L, "Cliente"), (6L, "Comercio") })
            Records[id] = new Usuario { Id = id, Nombre = $"Usuario {id}", Email = $"user{id}@example.test",
                Rol = role, Activo = id != 5, PasswordHash = passwordHash, Token = "private-recovery-token",
                Codigo = "private-code", CodigoExpiracion = DateTime.UtcNow.AddMinutes(10), ComercioId = id == 4 ? 10 : null };

        Users.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((long id) => Records.GetValueOrDefault(id));
        Users.Setup(r => r.GetByCorreoAsync(It.IsAny<string>())).ReturnsAsync((string email) =>
            Records.Values.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)));
        Users.Setup(r => r.CreateAsync(It.IsAny<Usuario>())).ReturnsAsync((Usuario user) => user);
        Users.Setup(r => r.UpdateAsync(It.IsAny<Usuario>())).Returns(Task.CompletedTask);
        Users.Setup(r => r.GetByIdComercioAndIdUserAsync(4, 10)).ReturnsAsync(Records[4]);
        Users.Setup(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(ApiResponse<PagedResponse<Usuario>>.Success(new PagedResponse<Usuario> { Page = 1, PageSize = 10,
                TotalItems = 1, TotalPages = 1, Items = new() { Records[3] } }));
        Plans.Setup(r => r.CreateAsync(It.IsAny<Plan>())).ReturnsAsync((Plan plan) => plan);
        Plans.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Plan { Id = 1, Nombre = "Prueba", Tipo = "FREE" });
        Plans.Setup(r => r.GetByTipoAsync(It.IsAny<string>())).ReturnsAsync(new Plan { Tipo = "BUSINESS", IsMultiUsuario = true });
        Plans.Setup(r => r.GetAllPlanesUser()).ReturnsAsync(new List<Plan> { new() { Nombre = "FREE", Tipo = "FREE" } });
        Refresh.Setup(r => r.GenerarRefreshTokenAsync(It.IsAny<long>(), It.IsAny<string?>()))
            .ReturnsAsync(("synthetic-refresh", new RefreshToken()));
        Comercios.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((long id) =>
            id is 10 or 20 ? new Comercio { Id = id, IdUsuario = id == 10 ? 3 : 6 } : null);
        Comercios.Setup(r => r.getAllColaboradores(10, 1, 10)).ReturnsAsync(
            ApiResponse<PagedResponse<UsuarioInfoDto>>.Success(new PagedResponse<UsuarioInfoDto>()));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            ["Jwt:Key"] = Key, ["Jwt:Issuer"] = "Phase1Test", ["Jwt:Audience"] = "Phase1Test" });
        var database = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(database));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<JwtContext>();
        builder.Services.AddSingleton(Users.Object);
        builder.Services.AddSingleton(Plans.Object);
        builder.Services.AddSingleton(Comercios.Object);
        builder.Services.AddSingleton(Refresh.Object);
        builder.Services.AddSingleton(new Mock<ISuscripcionRepository>().Object);
        builder.Services.AddSingleton(new Mock<IUsoCodigoReferidoRepository>().Object);
        builder.Services.AddSingleton(new Mock<IEmailService>().Object);
        builder.Services.AddSingleton(new Mock<IRelComercioImagenRepositorio>().Object);
        builder.Services.AddSingleton(new Mock<IHorarioComercioRepository>().Object);
        builder.Services.AddSingleton(new Mock<ITipoComercioRepository>().Object);
        builder.Services.AddSingleton(new Mock<IGeoLocationService>().Object);
        builder.Services.AddSingleton(new Mock<IProductosServiciosRepository>().Object);
        builder.Services.AddSingleton(new Mock<ILocationRepository>().Object);
        builder.Services.AddSingleton(new Mock<ICalificacionComentarioRepository>().Object);
        builder.Services.AddScoped<IUsuarioService, UsuarioService>();
        builder.Services.AddScoped<IPlanService, PlanService>();
        builder.Services.AddScoped<IComercioService, ComercioService>();
        builder.Services.AddScoped<ITipoComercioService, TipoComercioService>();
        builder.Services.AddControllers().AddApplicationPart(typeof(AdminController).Assembly);
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => {
            o.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
                ValidIssuer = "Phase1Test", ValidAudience = "Phase1Test", IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)) };
            o.Events = TokenSecurityValidator.CrearEventos();
        });
        builder.Services.AddAuthorization();
        _app = builder.Build();
        using (var scope = _app.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Usuarios.AddRange(Records.Values);
            await db.SaveChangesAsync();
        }
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();
        await _app.StartAsync();
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        Client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public string Token(long userId, bool includeId = true, string? claimedRole = null, string? additionalRole = null)
    {
        var role = claimedRole ?? Records[userId].Rol;
        var claims = new List<Claim> { new("rol", role), new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64) };
        if (includeId) claims.Add(new("id", userId.ToString()));
        if (additionalRole != null) claims.Add(new(ClaimTypes.Role, additionalRole));
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("Phase1Test", "Phase1Test", claims,
            DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddMinutes(10),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256)));
    }

    public Task<HttpResponseMessage> Send(string method, string route, long? userId, object? body = null, string? token = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (userId.HasValue || token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? Token(userId!.Value));
        if (body != null) request.Content = JsonContent.Create(body);
        return Client.SendAsync(request);
    }

    public async Task DisposeAsync() { Client.Dispose(); await _app.DisposeAsync(); }
}

public class Fase1SecurityHttpTests : IClassFixture<Fase1SecurityHost>
{
    private readonly Fase1SecurityHost _host;
    public Fase1SecurityHttpTests(Fase1SecurityHost host) => _host = host;
    private static object AdminBody => new { nombre = "Nuevo", email = "new@example.test", password = "PasswordSeguro123!" };
    private static object PlanBody => new { nombre = "Plan", tipo = "FREE", duracionDias = 30, precio = 0 };

    [Theory]
    [InlineData(null, 401)] [InlineData(1L, 403)] [InlineData(3L, 403)] [InlineData(4L, 403)]
    public async Task CrearAdmin_RechazaAnonimosYRolesSinPermiso(long? caller, int expected)
    {
        _host.Users.Invocations.Clear();
        using var response = await _host.Send("POST", "/api/Admin/crear", caller, AdminBody);
        Assert.Equal(expected, (int)response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expected.ToString(), json.GetProperty("codigo").GetString());
        _host.Users.Verify(r => r.CreateAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task CrearAdmin_AdministradorAutenticado_ConservaAltaLegitima()
    {
        using var response = await _host.Send("POST", "/api/Admin/crear", 2, AdminBody);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Admin", json.GetProperty("respuesta").GetProperty("rol").GetString());
    }

    [Theory]
    [InlineData("GET", "/api/Usuarios")]
    [InlineData("GET", "/api/Usuarios/2")]
    [InlineData("DELETE", "/api/Usuarios/2")]
    [InlineData("PATCH", "/api/Usuarios/2/estado")]
    [InlineData("PATCH", "/api/Usuarios/1/rol")]
    [InlineData("POST", "/api/Usuarios/2/revocar-sesiones")]
    [InlineData("GET", "/api/Usuarios/2/sesiones")]
    [InlineData("POST", "/api/Planes")]
    [InlineData("PUT", "/api/Planes/1")]
    [InlineData("DELETE", "/api/Planes/1")]
    [InlineData("POST", "/api/Configuracion/probar-correo")]
    [InlineData("POST", "/api/TiposComercio")]
    [InlineData("PUT", "/api/TiposComercio/1")]
    [InlineData("DELETE", "/api/TiposComercio/1")]
    public async Task AccionesAdministrativas_SoloAdmin(string method, string route)
    {
        foreach (var id in new long?[] { null, 1, 3, 4 }) {
            using var response = await _host.Send(method, route, id, new { activo = false, nuevoRol = "Admin", email = "test@example.test" });
            Assert.Equal(id == null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("POST")] [InlineData("PUT")] [InlineData("DELETE")]
    public async Task Planes_AdminPuedeGestionar(string method)
    {
        using var response = await _host.Send(method, method == "POST" ? "/api/Planes" : "/api/Planes/1", 2, PlanBody);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Auth", 1L)] [InlineData("Usuario", 3L)] [InlineData("Admin", 2L)]
    public async Task ActualizarJwt_RechazaOtraIdentidadYConservaLaPropia(string alias, long id)
    {
        using var forbidden = await _host.Send("POST", $"/api/{alias}/actualizar-jwt", id,
            new { email = "user6@example.test", updateJWT = true });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var own = await _host.Send("POST", $"/api/{alias}/actualizar-jwt", id,
            new { email = $"  USER{id}@example.test  ", updateJWT = true });
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        var json = await own.Content.ReadFromJsonAsync<JsonElement>();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(json.GetProperty("respuesta").GetProperty("token").GetString());
        Assert.Equal(id.ToString(), jwt.Claims.Single(c => c.Type == "id").Value);
        Assert.Equal(_host.Records[id].Rol, jwt.Claims.Single(c => c.Type == "rol").Value);
        using var metadata = await _host.Send("POST", $"/api/{alias}/actualizar-jwt", id,
            new { email = _host.Records[id].Email, updateJWT = false });
        Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
        var metadataJson = await metadata.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, metadataJson.GetProperty("respuesta").GetProperty("token").ValueKind);
    }

    [Theory]
    [InlineData(1L, false, null)] [InlineData(1L, true, "Admin")] [InlineData(5L, true, null)]
    public async Task JwtSinIdentidadConRolFalsoOCuentaBloqueada_NoSeAcepta(long id, bool includeId, string? role)
    {
        using var response = await _host.Send("GET", "/api/Auth", null, token: _host.Token(id, includeId, role));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task JwtConRolesDuplicados_NoPuedeEvitarLaComprobacionDelRolEnBaseDeDatos()
    {
        using var response = await _host.Send("POST", "/api/Admin/crear", null, AdminBody,
            _host.Token(3, additionalRole: "Admin"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/Usuarios/3")] [InlineData("/api/Usuarios")]
    public async Task UsuariosAdmin_DtoNoExponeCredenciales(string route)
    {
        using var response = await _host.Send("GET", route, 2);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<JsonElement>();
        var json = envelope.GetProperty("respuesta").GetRawText();
        foreach (var name in new[] { "passwordHash", "token", "codigo", "codigoExpiracion", "intentosCodigo", "suscripciones", "direcciones", "comercios" })
            Assert.DoesNotContain($"\"{name}\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("user3@example.test", json);
    }

    [Fact]
    public void UsuarioAnidado_NoSerializaCredenciales()
    {
        var json = JsonSerializer.Serialize(new { usuario = _host.Records[3] });
        Assert.DoesNotContain("private-recovery-token", json);
        Assert.DoesNotContain("private-code", json);
        Assert.DoesNotContain(_host.Records[3].PasswordHash, json);
    }

    [Fact]
    public async Task RegistroPublico_NoPermiteAsignarComercioAjeno()
    {
        using var response = await _host.Send("POST", "/api/Usuario/crear", null,
            new { nombre = "Ataque", email = "new@example.test", password = "PasswordSeguro123!", comercioId = 10 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "getAllColaboradores")]
    [InlineData("PUT", "toggleAccesoColaborador")]
    [InlineData("DELETE", "eliminarColaborador")]
    [InlineData("POST", "guardarColaborador")]
    public async Task Colaboradores_RechazaComercioAjenoSinLeerNiModificarUsuario(string method, string action)
    {
        _host.Users.Invocations.Clear();
        using var response = await _host.Send(method, $"/api/Comercios/{action}?idComercio=10&idColaborador=4", 6,
            new { idComercio = 10, nombre = "Nuevo", correo = "new@example.test" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(_host.Users.Invocations);
    }

    [Fact]
    public async Task Colaboradores_DuenoConservaGestionDeSuComercio()
    {
        using var response = await _host.Send("GET", "/api/Comercios/getAllColaboradores?idComercio=10", 3);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CatalogoPublicoYLoginLegitimo_SiguenDisponibles()
    {
        using var catalog = await _host.Send("GET", "/api/Planes/AllPlanesUser", null);
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        using var login = await _host.Send("POST", "/api/Auth/login", null,
            new { email = "user3@example.test", password = "SyntheticPassword123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("respuesta").GetProperty("token").GetString()!;
        using var own = await _host.Send("GET", "/api/Auth", null, token: token);
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        using var denied = await _host.Send("POST", "/api/Admin/crear", null, AdminBody, token);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Theory]
    [InlineData(null, "401")] [InlineData("Cliente", "403")] [InlineData("Comercio", "403")]
    public async Task Servicios_NoPermitenSaltarAutorizacionInvocandolosDirectamente(string? role, string expected)
    {
        using var scope = _host.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(role == null ? new ClaimsIdentity()
            : new ClaimsIdentity(new[] { new Claim("id", "3"), new Claim("rol", role), new Claim(ClaimTypes.Role, role) }, "Test")) };
        try {
            _host.Users.Invocations.Clear(); _host.Plans.Invocations.Clear();
            var service = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
            Assert.Equal(expected, (await service.CrearAdmin(new AdminCreateDto())).Codigo);
            Assert.Equal(expected, (await service.GetUsuarioById(2)).Codigo);
            Assert.Equal(expected, (await service.GetAllUsuarios(1, 10, "recent", "")).Codigo);
            Assert.Equal(expected, (await service.CambiarEstadoUsuario(2, false)).Codigo);
            Assert.Equal(expected, (await service.CambiarRolUsuario(3, "Admin")).Codigo);
            Assert.Equal(expected, (await service.DeleteUsuario(2)).Codigo);
            var plans = scope.ServiceProvider.GetRequiredService<IPlanService>();
            Assert.Equal(expected, (await plans.CrearPlan(new PlanCreateDto())).Codigo);
            Assert.Equal(expected, (await plans.ActualizarPlan(1, new PlanCreateDto())).Codigo);
            Assert.Equal(expected, (await plans.EliminarPlan(1)).Codigo);
            Assert.Empty(_host.Users.Invocations); Assert.Empty(_host.Plans.Invocations);
        }
        finally { accessor.HttpContext = previous; }
    }

    [Fact]
    public async Task CambiarRol_AdminRechazaRolesInventados()
    {
        using var response = await _host.Send("PATCH", "/api/Usuarios/3/rol", 2, new { nuevoRol = "Administrador" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Comercio", _host.Records[3].Rol);
    }
}
