using System.Security.Claims;
using System.Text.Json;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Integration;

// Estos son tests del servicio con mocks; la concurrencia real está en CheckoutPostgresTests.
public class CheckoutPipelineIntegrationTests
{
    private sealed class Fixture
    {
        public readonly Mock<IPedidoRepository> Repo = new();
        public readonly Mock<INotificacionService> Notificaciones = new();
        public readonly Carrito Carrito = new() { Id = 10, IdUsuario = 5 };
        public readonly List<CheckoutCarritoItemDto> Items;
        public readonly ConfirmarCheckoutDto Dto;
        public readonly CheckoutService Service;
        public List<Pedido>? Guardados;
        public Fixture()
        {
            var comercio = new Comercio { Id = 1, Nombre = "Tienda sintética" };
            Items = [new() { IdDetalleCarrito = 1, IdProductoServicio = 50, Cantidad = 2,
                Comercio = comercio, Producto = new() { Id = 50, IdComercio = 1,
                    Nombre = "Producto sintético", Precio = 45, ManejaStock = true, Stock = 10 } }];
            Dto = new() { IdempotencyKey = Guid.NewGuid().ToString(), CarritoUuid = Carrito.Uuid,
                CarritoVersion = CheckoutOperacion.Carrito(Items),
                Comercios = [new() { ComercioUuid = comercio.Uuid, MetodoPago = MetodoPagoPedido.Efectivo,
                    TipoEntrega = TipoEntregaPedido.Recoger }] };
            Repo.Setup(r => r.ObtenerUsuarioAsync(5)).ReturnsAsync(new Usuario { Id = 5, Nombre = "Cliente sintético" });
            Repo.Setup(r => r.ObtenerCarritoActivoAsync(5)).ReturnsAsync(Carrito);
            Repo.Setup(r => r.ObtenerProductosCarritoAsync(10)).ReturnsAsync(Items);
            Repo.Setup(r => r.ObtenerConfiguracionPagoAsync(1)).ReturnsAsync(new ConfiguracionPagoComercio { AceptaEfectivo = true });
            Repo.Setup(r => r.GuardarCheckoutAsync(It.IsAny<List<Pedido>>(), It.IsAny<List<(long, int, string)>>(),
                It.IsAny<Carrito>(), It.IsAny<CheckoutIdempotencia>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<Pedido> pedidos, List<(long,int,string)> stock, Carrito cart,
                    CheckoutIdempotencia idempotencia, string json, string version, CancellationToken token) =>
                    { Guardados = pedidos; return new CheckoutGuardado(json, true); });
            var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("id", "5"), new Claim("rol", "Cliente"), new Claim(ClaimTypes.Role, "Cliente")], "test")) };
            Service = new(Repo.Object, new JwtContext(new HttpContextAccessor { HttpContext = context }),
                Notificaciones.Object, NullLogger<CheckoutService>.Instance);
        }
        public void Cache(string status = "completed", string? json = null, DateTime? fecha = null)
        {
            Repo.Setup(r => r.ObtenerIdempotenciaAsync(5, Dto.IdempotencyKey!)).ReturnsAsync(new CheckoutIdempotencia {
                IdUsuario = 5, IdempotencyKey = Dto.IdempotencyKey!, RequestHash = CheckoutOperacion.Solicitud(Dto),
                Status = status, ResponseJson = json ?? JsonSerializer.Serialize(new ConfirmarCheckoutResponseDto { TotalPedidos = 1, TotalGeneral = 90 }),
                FechaCreacion = fecha ?? DateTime.UtcNow });
        }
        public void SinPersistencia() => Assert.Null(Guardados);
    }

    [Fact] public async Task Confirmar_CarritoVacio_Retorna400()
    {
        var f = new Fixture(); f.Items.Clear();
        var r = await f.Service.Confirmar(f.Dto);
        Assert.Equal("400", r.Codigo); Assert.Equal("Tu carrito está vacío.", r.Mensaje); f.SinPersistencia();
    }
    [Fact] public async Task Confirmar_FaltaConfigurarComercio_Retorna400SinRegistrarIntento()
    {
        var f = new Fixture(); f.Dto.Comercios.Clear();
        Assert.Equal("400", (await f.Service.Confirmar(f.Dto)).Codigo); f.SinPersistencia();
    }
    [Fact] public async Task Confirmar_CompletadaSinCarrito_RecuperaRespuesta()
    {
        var f = new Fixture(); f.Cache();
        f.Repo.Setup(r => r.ObtenerCarritoActivoAsync(5)).ReturnsAsync((Carrito?)null);
        var r = await f.Service.Confirmar(f.Dto);
        Assert.Equal("200", r.Codigo); Assert.Equal(90, r.Respuesta!.TotalGeneral);
        f.Repo.Verify(r => r.ObtenerCarritoActivoAsync(5), Times.Never); f.SinPersistencia();
    }
    [Fact] public async Task Confirmar_MismaClaveConPayloadDistinto_Retorna409()
    {
        var f = new Fixture(); f.Cache(); f.Dto.Comercios[0].Observaciones = "Cambio";
        Assert.Equal("409", (await f.Service.Confirmar(f.Dto)).Codigo); f.SinPersistencia();
    }
    [Fact] public async Task Confirmar_ProcessingReciente_Retorna409()
    {
        var f = new Fixture(); f.Cache("processing");
        Assert.Equal("409", (await f.Service.Confirmar(f.Dto)).Codigo); f.SinPersistencia();
    }
    [Fact] public async Task Confirmar_ProcessingVencido_PermiteRecuperacionTransaccional()
    {
        var f = new Fixture(); f.Cache("processing", fecha: DateTime.UtcNow.AddHours(-1));
        Assert.Equal("200", (await f.Service.Confirmar(f.Dto)).Codigo); Assert.Single(f.Guardados!);
    }
    [Fact] public async Task Confirmar_SinConfiguracion_NoBloqueaReintento()
    {
        var f = new Fixture(); f.Repo.Setup(r => r.ObtenerConfiguracionPagoAsync(1)).ReturnsAsync((ConfiguracionPagoComercio?)null);
        Assert.Equal("400", (await f.Service.Confirmar(f.Dto)).Codigo); f.SinPersistencia();
        f.Repo.Setup(r => r.ObtenerConfiguracionPagoAsync(1)).ReturnsAsync(new ConfiguracionPagoComercio { AceptaEfectivo = true });
        Assert.Equal("200", (await f.Service.Confirmar(f.Dto)).Codigo);
    }
    [Fact] public async Task Confirmar_Exitoso_NotificaUnaVez()
    {
        var f = new Fixture(); Assert.Equal("200", (await f.Service.Confirmar(f.Dto)).Codigo);
        Assert.Single(f.Guardados!); Assert.Equal(90, f.Guardados![0].Total);
        f.Notificaciones.Verify(n => n.NotificarComercioAsync(It.IsAny<Pedido>(), TipoNotificacionPedido.PedidoCreado,
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
    [Fact] public async Task Confirmar_CarritoCambio_Retorna400()
    {
        var f = new Fixture(); f.Items[0].Cantidad++;
        Assert.Equal("400", (await f.Service.Confirmar(f.Dto)).Codigo); f.SinPersistencia();
    }
    [Fact] public async Task Confirmar_ClaveAusente_Retorna400()
    {
        var f = new Fixture(); f.Dto.IdempotencyKey = null;
        Assert.Equal("400", (await f.Service.Confirmar(f.Dto)).Codigo); f.SinPersistencia();
    }
    [Fact] public async Task Confirmar_Cancelacion_NoSeConvierteEn500()
    {
        var f = new Fixture(); using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Service.Confirmar(f.Dto, cts.Token));
        f.SinPersistencia();
    }
    [Fact] public async Task Confirmar_FalloPersistencia_NoExponeDetallesInternos()
    {
        var f = new Fixture();
        f.Repo.Setup(r => r.GuardarCheckoutAsync(It.IsAny<List<Pedido>>(), It.IsAny<List<(long,int,string)>>(),
            It.IsAny<Carrito>(), It.IsAny<CheckoutIdempotencia>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("detalle privado de conexión"));
        var r = await f.Service.Confirmar(f.Dto); Assert.Equal("500", r.Codigo);
        Assert.DoesNotContain("privado", r.Mensaje);
    }
}
