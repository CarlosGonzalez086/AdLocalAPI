using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using AdLocalAPI.Filters;
using AdLocalAPI.Validators;
using AdLocalAPI.Repositories.Interfaces;
using System.Data.Common;
using System.Security.Claims;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using Xunit;

namespace AdLocalAPI.Tests.Integration;

public abstract class CheckoutPostgresFixture : IAsyncLifetime
{
    protected string connection = "";
    protected string adminConnection = "";
    protected readonly string database = "adlocal_checkout_test_" + Guid.NewGuid().ToString("N");
    protected readonly Guid cartUuid = Guid.NewGuid();
    protected Guid comercioUuid;
    protected bool creada;

    protected AppDbContext Context(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(connection, x => x.UseNetTopologySuite()).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
        .AddInterceptors(interceptors).Options);

    public async Task InitializeAsync()
    {
        var raw = Environment.GetEnvironmentVariable("ADLOCAL_TEST_POSTGRES");
        if (string.IsNullOrEmpty(raw)) return;
        var cs = new NpgsqlConnectionStringBuilder(raw);
        if (cs.Host is not ("127.0.0.1" or "localhost" or "::1"))
            throw new InvalidOperationException("Las pruebas destructivas solo permiten PostgreSQL en loopback.");
        cs.Database = "postgres"; adminConnection = cs.ConnectionString;
        await using var admin = new NpgsqlConnection(adminConnection); await admin.OpenAsync();
        await using (var cmd = new NpgsqlCommand($"CREATE DATABASE {database}", admin)) await cmd.ExecuteNonQueryAsync();
        creada = true; cs.Database = database; connection = cs.ConnectionString;
        await using var db = Context(); await db.Database.EnsureCreatedAsync();
        var estado = new Estado { EstadoNombre = "Estado sintÃ©tico" };
        var municipio = new Municipio { MunicipioNombre = "Municipio sintÃ©tico" };
        db.AddRange(estado, municipio); await db.SaveChangesAsync();
        db.Usuarios.AddRange(new Usuario { Id = 5, Nombre = "Cliente", Email = "cliente@example.invalid", Rol = "Cliente" },
            new Usuario { Id = 6, Nombre = "Cliente 2", Email = "cliente2@example.invalid", Rol = "Cliente" },
            new Usuario { Id = 7, Nombre = "Comercio", Email = "comercio@example.invalid", Rol = "Comercio" });
        await db.SaveChangesAsync();
        var comercio = new Comercio { Id = 1, Nombre = "Tienda sintÃ©tica", IdUsuario = 7, EstadoId = estado.Id, MunicipioId = municipio.Id };
        comercioUuid = comercio.Uuid; db.Comercios.Add(comercio); await db.SaveChangesAsync();
        db.ProductosServicios.Add(new() { Id = 50, IdComercio = 1, IdUsuario = 7,
            Nombre = "Producto", Precio = 45, Stock = 10, ManejaStock = true, Tipo = TipoProductoServicio.Producto });
        await db.SaveChangesAsync();
        db.Carritos.Add(new() { Id = 10, Uuid = cartUuid, IdUsuario = 5, Subtotal = 90 }); await db.SaveChangesAsync();
        db.CarritoDetalles.Add(new() { Id = 20, IdCarrito = 10, IdProductoServicio = 50, Cantidad = 2, PrecioUnitario = 45, Subtotal = 90 });
        db.ConfiguracionPagoComercios.Add(new() { IdComercio = 1, AceptaEfectivo = true }); await db.SaveChangesAsync();
    }
    public async Task DisposeAsync()
    {
        if (!creada) return;
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(adminConnection); await admin.OpenAsync();
        // database proviene Ãºnicamente del prefijo fijo y un GUID generado por esta clase.
        await using var cmd = new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", admin);
        await cmd.ExecuteNonQueryAsync();
    }
    protected CheckoutService Service(AppDbContext db, long usuario = 5)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("id", usuario.ToString()), new Claim("rol", "Cliente"), new Claim(ClaimTypes.Role, "Cliente")], "test")) };
        return new(new PedidoRepository(db), new JwtContext(new HttpContextAccessor { HttpContext = context }),
            new Mock<INotificacionService>().Object, NullLogger<CheckoutService>.Instance);
    }
    protected async Task<ConfirmarCheckoutDto> Request(long usuario = 5)
    {
        await using var db = Context(); var checkout = (await Service(db, usuario).ObtenerCheckout()).Respuesta!;
        return new() { IdempotencyKey = Guid.NewGuid().ToString(), CarritoUuid = checkout.CarritoUuid,
            CarritoVersion = checkout.CarritoVersion, Comercios = [new() { ComercioUuid = comercioUuid,
                MetodoPago = MetodoPagoPedido.Efectivo, TipoEntrega = TipoEntregaPedido.Recoger }] };
    }
    protected async Task<ApiResponse<ConfirmarCheckoutResponseDto>> Confirm(ConfirmarCheckoutDto dto, long usuario = 5, params IInterceptor[] interceptors)
    {
        await using var db = Context(interceptors); return await Service(db, usuario).Confirmar(dto);
    }
    protected async Task AssertState(int pedidos, int stock, bool activo, int idempotencias)
    {
        await using var db = Context(); Assert.Equal(pedidos, await db.Pedidos.CountAsync());
        Assert.Equal(stock, (await db.ProductosServicios.SingleAsync(x => x.Id == 50)).Stock);
        Assert.Equal(activo, (await db.Carritos.SingleAsync(x => x.Id == 10)).Activo);
        Assert.Equal(idempotencias, await db.CheckoutIdempotencias.CountAsync());
    }

}
