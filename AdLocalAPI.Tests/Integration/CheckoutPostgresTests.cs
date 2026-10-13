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

public sealed class PostgresCheckoutFactAttribute : FactAttribute
{
    public PostgresCheckoutFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ADLOCAL_TEST_POSTGRES")))
            Skip = "Requiere PostgreSQL/PostGIS local: ADLOCAL_TEST_POSTGRES. No sustituir por InMemory.";
    }
}

// Cada caso crea y elimina EXCLUSIVAMENTE su base con nombre aleatorio de prueba.
public class CheckoutPostgresTests : CheckoutPostgresFixture
{
    [PostgresCheckoutFact] public async Task Commit_RespuestaPerdida_ReintentoSinCarrito_RecuperaMismosPedidos()
    {
        var dto = await Request(); var first = await Confirm(dto); var retry = await Confirm(dto);
        Assert.Equal("200", first.Codigo); Assert.Equal("200", retry.Codigo);
        Assert.Equal(first.Respuesta!.Pedidos[0].Uuid, retry.Respuesta!.Pedidos[0].Uuid);
        await AssertState(1, 8, false, 1);
    }
    [PostgresCheckoutFact] public async Task ValidacionFallida_Corregir_ReintentarMismaClave()
    {
        var dto = await Request();
        await using (var db = Context()) await db.ConfiguracionPagoComercios.ExecuteDeleteAsync();
        Assert.Equal("400", (await Confirm(dto)).Codigo); await AssertState(0, 10, true, 0);
        await using (var db = Context()) { db.ConfiguracionPagoComercios.Add(new() { IdComercio = 1, AceptaEfectivo = true }); await db.SaveChangesAsync(); }
        Assert.Equal("200", (await Confirm(dto)).Codigo); await AssertState(1, 8, false, 1);
    }
    [PostgresCheckoutFact] public async Task FalloDespuesDeStock_RollbackCompleto_YReintento()
    {
        var dto = await Request(); Assert.Equal("500", (await Confirm(dto, 5, new FallarPedido())).Codigo);
        await AssertState(0, 10, true, 0);
        Assert.Equal("200", (await Confirm(dto)).Codigo); await AssertState(1, 8, false, 1);
    }
    [PostgresCheckoutFact] public async Task MismaClaveEnParalelo_CreaSoloUnPedido()
    {
        var dto = await Request(); var gate = new DosSolicitudes();
        var results = await Task.WhenAll(Confirm(dto, 5, gate), Confirm(dto, 5, gate));
        Assert.All(results, r => Assert.Equal("200", r.Codigo));
        Assert.Equal(results[0].Respuesta!.Pedidos[0].Uuid, results[1].Respuesta!.Pedidos[0].Uuid);
        await AssertState(1, 8, false, 1);
    }
    [PostgresCheckoutFact] public async Task DosClavesMismoCarritoEnParalelo_NoDuplican()
    {
        var dto1 = await Request(); var dto2 = await Request(); var gate = new DosSolicitudes();
        var results = await Task.WhenAll(Confirm(dto1, 5, gate), Confirm(dto2, 5, gate));
        Assert.Single(results, r => r.Codigo == "200"); Assert.Single(results, r => r.Codigo == "409");
        await AssertState(1, 8, false, 1);
    }
    [PostgresCheckoutFact] public async Task DosClientesUltimoStock_UnoCompra_OtroNo()
    {
        await using (var db = Context())
        {
            await db.ProductosServicios.ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, 2));
            db.Carritos.Add(new() { Id = 11, IdUsuario = 6, Subtotal = 90 }); await db.SaveChangesAsync();
            db.CarritoDetalles.Add(new() { IdCarrito = 11, IdProductoServicio = 50, Cantidad = 2, PrecioUnitario = 45, Subtotal = 90 }); await db.SaveChangesAsync();
        }
        var dto1 = await Request(); var dto2 = await Request(6); var gate = new DosSolicitudes();
        var results = await Task.WhenAll(Confirm(dto1, 5, gate), Confirm(dto2, 6, gate));
        Assert.Single(results, r => r.Codigo == "200"); Assert.Single(results, r => r.Codigo == "400");
        await using var verify = Context(); Assert.Equal(1, await verify.Pedidos.CountAsync());
        Assert.Equal(0, (await verify.ProductosServicios.SingleAsync()).Stock);
        Assert.Equal(1, await verify.CheckoutIdempotencias.CountAsync());
        Assert.Equal(1, await verify.Carritos.CountAsync(x => x.Activo));
    }
    [PostgresCheckoutFact] public async Task MismaClavePayloadDistinto_NoRecuperaNiCreaOtraCompra()
    {
        var dto = await Request(); Assert.Equal("200", (await Confirm(dto)).Codigo);
        dto.Comercios[0].Observaciones = "Otra entrega";
        Assert.Equal("409", (await Confirm(dto)).Codigo); await AssertState(1, 8, false, 1);
    }
    [PostgresCheckoutFact] public async Task ProcessingLegadoVencido_SeRecuperaSinOtraFila()
    {
        var dto = await Request(); await using (var db = Context()) {
            db.CheckoutIdempotencias.Add(new() { IdUsuario = 5, IdempotencyKey = dto.IdempotencyKey!,
                Status = "processing", FechaCreacion = DateTime.UtcNow.AddDays(-1) }); await db.SaveChangesAsync(); }
        Assert.Equal("200", (await Confirm(dto)).Codigo); await AssertState(1, 8, false, 1);
    }
    [PostgresCheckoutFact] public async Task CambiarCantidadDesdeOtroContexto_RechazaVersionVieja()
    {
        var dto = await Request(); await using (var db = Context())
            await db.CarritoDetalles.ExecuteUpdateAsync(s => s.SetProperty(x => x.Cantidad, 3));
        Assert.Equal("400", (await Confirm(dto)).Codigo); await AssertState(0, 10, true, 0);
    }
    [PostgresCheckoutFact] public async Task EscrituraTardia_NoReabreCarritoNiAgregaDetalle()
    {
        var dto = await Request(); await using var stale = Context(); var carrito = await stale.Carritos.SingleAsync();
        var detalle = await stale.CarritoDetalles.SingleAsync(); Assert.Equal("200", (await Confirm(dto)).Codigo);
        var repo = new CarritoRepository(stale);
        await Assert.ThrowsAsync<CheckoutConflictException>(() => repo.ActualizarCarritoAsync(carrito));
        await Assert.ThrowsAsync<CheckoutConflictException>(() => repo.ActualizarDetalleAsync(detalle));
        await AssertState(1, 8, false, 1);
    }
    [PostgresCheckoutFact] public async Task ControlDeStockCambia_RechazaVersionAnterior()
    {
        await using (var db = Context())
            await db.ProductosServicios.ExecuteUpdateAsync(s => s.SetProperty(x => x.ManejaStock, false));
        var dto = await Request();
        await using (var db = Context())
            await db.ProductosServicios.ExecuteUpdateAsync(s => s.SetProperty(x => x.ManejaStock, true));
        Assert.Equal("400", (await Confirm(dto)).Codigo);
        await AssertState(0, 10, true, 0);
    }
    [PostgresCheckoutFact] public async Task ComercioOculto_NoCreaPedido()
    {
        await using (var db = Context())
            await db.Comercios.ExecuteUpdateAsync(s => s.SetProperty(x => x.Visible, false));
        var dto = await Request();
        Assert.Equal("400", (await Confirm(dto)).Codigo);
        await AssertState(0, 10, true, 0);
    }
    [PostgresCheckoutFact] public async Task Migracion_RequestHash_ConservaFilasExistentes()
    {
        await using var db = Context(); db.CheckoutIdempotencias.Add(new() { IdUsuario = 5, IdempotencyKey = "legado", Status = "processing" }); await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE checkout_idempotencias DROP COLUMN \"RequestHash\";");
        await db.Database.ExecuteSqlRawAsync(db.GetService<IHistoryRepository>().GetCreateScript());
        var script = db.GetService<IMigrator>().GenerateScript("20260924032004_TokensRevocadosAntesDe", "20261012211320_CheckoutRequestHash", MigrationsSqlGenerationOptions.Idempotent);
        await db.Database.ExecuteSqlRawAsync(script);
        await db.Database.ExecuteSqlRawAsync(script);
        var record = await db.CheckoutIdempotencias.SingleAsync(); Assert.Equal("legado", record.IdempotencyKey); Assert.Null(record.RequestHash);
    }
    [PostgresCheckoutFact] public async Task ContratoHttp_JwtValidacion_HeaderYCuerpo_RecuperaPedido()
    {
        var dto = await Request();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers(o => o.Filters.Add<ValidationFilter>())
            .AddApplicationPart(typeof(AdLocalAPI.Controllers.CheckoutController).Assembly);
        builder.Services.AddValidatorsFromAssemblyContaining<ConfirmarCheckoutDtoValidator>();
        builder.Services.AddHttpContextAccessor(); builder.Services.AddScoped<JwtContext>();
        builder.Services.AddScoped(_ => Context()); builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
        builder.Services.AddScoped<ICheckoutService, CheckoutService>();
        builder.Services.AddSingleton(new Mock<INotificacionService>().Object);
        const string key = "Phase2SyntheticKeyForHttpContractOnly123456789!";
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
            o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = false, ValidateAudience = false,
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)) });
        builder.Services.AddAuthorization();
        await using var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Checkout")).StatusCode);
        var token = new JwtSecurityToken(claims: [new Claim("id", "5"), new Claim("rol", "Cliente"), new Claim(ClaimTypes.Role, "Cliente")],
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        var keyOriginal = dto.IdempotencyKey!;
        client.DefaultRequestHeaders.Add("Idempotency-Key", "distinta");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Checkout/confirmar", dto)).StatusCode);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        var first = await client.PostAsJsonAsync("/api/Checkout/confirmar", dto); Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        dto.IdempotencyKey = null; client.DefaultRequestHeaders.Add("Idempotency-Key", keyOriginal);
        var retry = await client.PostAsJsonAsync("/api/Checkout/confirmar", dto); Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var a = await first.Content.ReadFromJsonAsync<ApiResponse<ConfirmarCheckoutResponseDto>>();
        var b = await retry.Content.ReadFromJsonAsync<ApiResponse<ConfirmarCheckoutResponseDto>>();
        Assert.Equal(a!.Respuesta!.Pedidos[0].Uuid, b!.Respuesta!.Pedidos[0].Uuid);
        await AssertState(1, 8, false, 1); await app.StopAsync();
    }
    [PostgresCheckoutFact] public async Task CancelacionAntesDeGuardarPedidos_RevierteStockEIdempotencia()
    {
        var dto = await Request(); using var cts = new CancellationTokenSource();
        await using (var db = Context(new CancelarPedido(cts)))
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(db).Confirmar(dto, cts.Token));
        await AssertState(0, 10, true, 0);
        Assert.Equal("200", (await Confirm(dto)).Codigo); await AssertState(1, 8, false, 1);
    }
    [PostgresCheckoutFact] public async Task CerrarCarritoVacio_SiguePermitido()
    {
        await using var db = Context(); var carrito = await db.Carritos.SingleAsync();
        await db.CarritoDetalles.ExecuteUpdateAsync(s => s.SetProperty(x => x.Activo, false));
        carrito.Activo = false; carrito.Subtotal = 0;
        await new CarritoRepository(db).ActualizarCarritoAsync(carrito);
        Assert.False((await db.Carritos.SingleAsync()).Activo);
    }
    private sealed class CancelarPedido(CancellationTokenSource cts) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO pedidos")) { cts.Cancel(); throw new OperationCanceledException(cts.Token); }
            return ValueTask.FromResult(result);
        }
    }
    private sealed class FallarPedido : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO pedidos")) throw new IOException("Fallo sintÃ©tico despuÃ©s de stock");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class DosSolicitudes : DbCommandInterceptor
    {
        private int numero;
        private readonly TaskCompletionSource ambos = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO checkout_idempotencias") && Interlocked.Increment(ref numero) <= 2)
            {
                if (numero >= 2) ambos.TrySetResult();
                await ambos.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }
}
