using System.Data.Common;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using AdLocalAPI.Filters;
using AdLocalAPI.Validators;
using AdLocalAPI.Repositories.Interfaces;
using Amazon.S3;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Integration;

public class PedidosOperacionesPostgresTests : CheckoutPostgresFixture
{
    private PedidoComercioService ServiceComercio(AppDbContext db, long actor = 7, string rol = "Comercio", bool fallaNotificacion = false)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("id", actor.ToString()), new Claim("rol", rol), new Claim(ClaimTypes.Role, rol)], "test")) };
        var notif = new Mock<INotificacionService>();
        if (fallaNotificacion) notif.Setup(n => n.NotificarClienteAsync(It.IsAny<Pedido>(), It.IsAny<TipoNotificacionPedido>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new IOException("Fallo sintético de notificación"));
        return new(new PedidoComercioRepository(db), new JwtContext(new HttpContextAccessor { HttpContext = context }),
            new Mock<IAmazonS3>().Object, new ConfigurationBuilder().Build(), notif.Object, new Mock<IComisionService>().Object,
            NullLogger<PedidoComercioService>.Instance);
    }
    private async Task<Guid> CrearPedido(EstadoPedido estado = EstadoPedido.PendienteAprobacion)
    {
        var result = await Confirm(await Request()); Assert.Equal("200", result.Codigo);
        var uuid = result.Respuesta!.Pedidos.Single().Uuid;
        await using var db = Context();
        await db.Pedidos.Where(x => x.Uuid == uuid).ExecuteUpdateAsync(s => s.SetProperty(x => x.Estado, estado)
            .SetProperty(x => x.MontoComision, 5).SetProperty(x => x.PorcentajeComision, 5));
        return uuid;
    }
    private async Task<ApiResponse<PedidoComercioDetalleDto>> Estado(Guid uuid, EstadoPedido estado, long version = 0, params IInterceptor[] interceptors)
    {
        await using var db = Context(interceptors);
        return await ServiceComercio(db).CambiarEstadoAsync(1, uuid, new() { Estado = estado, VersionPedido = version, Comentario = "Motivo sintético" });
    }
    private async Task<ApiResponse<PedidoComercioDetalleDto>> Pago(Guid uuid, long version = 0)
    {
        await using var db = Context();
        return await ServiceComercio(db).RevisarPagoAsync(1, uuid, new() { EstadoPago = EstadoPagoPedido.Pagado, VersionPedido = version });
    }
    private RegistrarReembolsoPedidoDto Reembolso(long version = 1) => new() { VersionPedido = version,
        IdempotencyKey = Guid.NewGuid(), Monto = 90, Metodo = MetodoPagoPedido.Transferencia,
        Referencia = "TRANSFERENCIA-SINTETICA-001", Motivo = "Devolución total realizada" };
    private async Task<ApiResponse<PedidoComercioDetalleDto>> Refund(Guid uuid, RegistrarReembolsoPedidoDto dto, params IInterceptor[] interceptors)
    {
        await using var db = Context(interceptors); return await ServiceComercio(db).ReembolsarAsync(1, uuid, dto);
    }
    private async Task AssertStock(int stock, int movimientos)
    {
        await using var db = Context(); Assert.Equal(stock, (await db.ProductosServicios.SingleAsync()).Stock);
        Assert.Equal(movimientos, await db.PedidoMovimientosInventario.CountAsync());
    }

    [PostgresCheckoutFact] public async Task Rechazo_RestituyeUnaVez_ReintentoNoDuplica()
    {
        var uuid = await CrearPedido();
        var ok = await Estado(uuid, EstadoPedido.Rechazado); Assert.Equal("200", ok.Codigo);
        Assert.False(ok.Respuesta!.InventarioPendiente); Assert.Equal(1, ok.Respuesta.Version);
        Assert.Equal("409", (await Estado(uuid, EstadoPedido.Rechazado)).Codigo);
        await AssertStock(10, 1);
    }
    [PostgresCheckoutFact] public async Task CicloRecoger_NoEntregaSinCobro_CompletaPagado()
    {
        var uuid = await CrearPedido(); Assert.Equal("200", (await Estado(uuid, EstadoPedido.Aprobado)).Codigo);
        Assert.Equal("200", (await Estado(uuid, EstadoPedido.Preparando, 1)).Codigo);
        Assert.Equal("200", (await Estado(uuid, EstadoPedido.ListoParaRecoger, 2)).Codigo);
        Assert.Equal("409", (await Estado(uuid, EstadoPedido.Entregado, 3)).Codigo);
        Assert.Equal("200", (await Pago(uuid, 3)).Codigo);
        Assert.Equal("200", (await Estado(uuid, EstadoPedido.Entregado, 4)).Codigo);
        Assert.Equal("200", (await Estado(uuid, EstadoPedido.Completado, 5)).Codigo);
        await AssertStock(8, 0);
        await using var db = Context(); var p = await db.Pedidos.SingleAsync();
        Assert.NotNull(p.FechaEntrega); Assert.NotNull(p.FechaFinalizacion); Assert.Equal(6, p.Version);
        Assert.Single(await db.Comisiones.ToListAsync());
    }
    [PostgresCheckoutFact] public async Task DosOperadores_CancelacionYCobro_NoMezclanResultados()
    {
        var uuid = await CrearPedido(EstadoPedido.Aprobado);
        var results = await Task.WhenAll(Estado(uuid, EstadoPedido.Cancelado), Pago(uuid));
        Assert.Single(results, x => x.Codigo == "200"); Assert.Single(results, x => x.Codigo == "409");
        await using var db = Context(); var p = await db.Pedidos.SingleAsync();
        if (p.Estado == EstadoPedido.Cancelado)
        { Assert.Equal(EstadoPagoPedido.Pendiente, p.EstadoPago); await AssertStock(10, 1); Assert.Empty(await db.Comisiones.ToListAsync()); }
        else { Assert.Equal(EstadoPagoPedido.Pagado, p.EstadoPago); await AssertStock(8, 0); Assert.Single(await db.Comisiones.ToListAsync()); }
    }
    [PostgresCheckoutFact] public async Task DosCobros_UnaComision_UnHistorialDePago()
    {
        var uuid = await CrearPedido(); var results = await Task.WhenAll(Pago(uuid), Pago(uuid));
        Assert.Single(results, x => x.Codigo == "200"); Assert.Single(results, x => x.Codigo == "409");
        await using var db = Context(); Assert.Single(await db.Comisiones.ToListAsync());
        Assert.Equal(1, await db.Set<PedidoHistorialEstado>().CountAsync(x => x.Evento == "Pago"));
    }
    [PostgresCheckoutFact] public async Task ReembolsoTotal_RespuestaPerdida_MismaClaveRecuperaYAnulaComisionPendiente()
    {
        var uuid = await CrearPedido(); Assert.Equal("200", (await Pago(uuid)).Codigo); var dto = Reembolso();
        var first = await Refund(uuid, dto); dto.Monto = 90.00m; var retry = await Refund(uuid, dto);
        Assert.Equal("200", first.Codigo); Assert.Equal("200", retry.Codigo);
        Assert.Equal(first.Respuesta!.Reembolso!.Uuid, retry.Respuesta!.Reembolso!.Uuid);
        Assert.Equal(EstadoPedido.Cancelado, first.Respuesta.Estado);
        Assert.Equal(EstadoPagoPedido.Reembolsado, first.Respuesta.EstadoPago);
        await AssertStock(10, 1);
        await using var db = Context(); Assert.Equal(1, await db.PedidoReembolsos.CountAsync());
        Assert.Equal((int)EstatusComision.Cancelada, (await db.Comisiones.SingleAsync()).Estatus);
        Assert.Equal(1, await db.Set<PedidoHistorialEstado>().CountAsync(x => x.Evento == "Reembolso"));
    }
    [PostgresCheckoutFact] public async Task Reembolso_MismaClaveConOtraReferencia_NoModificaRegistro()
    {
        var uuid = await CrearPedido(); await Pago(uuid); var dto = Reembolso(); Assert.Equal("200", (await Refund(uuid, dto)).Codigo);
        dto.Referencia = "OTRA-REFERENCIA"; Assert.Equal("409", (await Refund(uuid, dto)).Codigo);
        await AssertStock(10, 1);
    }
    [PostgresCheckoutFact] public async Task ReembolsosConcurrentes_ClavesDistintas_UnSoloEfecto()
    {
        var uuid = await CrearPedido(); await Pago(uuid);
        var results = await Task.WhenAll(Refund(uuid, Reembolso()), Refund(uuid, Reembolso()));
        Assert.Single(results, x => x.Codigo == "200"); Assert.Single(results, x => x.Codigo == "409");
        await AssertStock(10, 1);
        await using var db = Context(); Assert.Single(await db.PedidoReembolsos.ToListAsync());
    }
    [PostgresCheckoutFact] public async Task ReembolsosConcurrentes_MismaClave_AmbosRecuperanMismoRegistro()
    {
        var uuid = await CrearPedido(); await Pago(uuid); var dto = Reembolso();
        var results = await Task.WhenAll(Refund(uuid, dto), Refund(uuid, dto));
        Assert.All(results, x => Assert.Equal("200", x.Codigo));
        Assert.Equal(results[0].Respuesta!.Reembolso!.Uuid, results[1].Respuesta!.Reembolso!.Uuid);
        await AssertStock(10, 1);
    }
    [PostgresCheckoutFact] public async Task Entregado_ReembolsoSinProductos_NoInventaStock()
    {
        var uuid = await CrearPedido(EstadoPedido.Completado); await Pago(uuid);
        var res = await Refund(uuid, Reembolso()); Assert.Equal("200", res.Codigo);
        Assert.Equal(EstadoPedido.Completado, res.Respuesta!.Estado); Assert.Empty(res.Respuesta.AccionesDisponibles);
        await AssertStock(8, 0);
    }
    [PostgresCheckoutFact] public async Task Entregado_DevuelveSoloUnidadesDeclaradas()
    {
        var uuid = await CrearPedido(EstadoPedido.Entregado); await Pago(uuid); var dto = Reembolso();
        await using (var db = Context()) dto.ProductosDevueltos.Add(new() { DetalleUuid = (await db.PedidoDetalles.SingleAsync()).Uuid, Cantidad = 1 });
        Assert.Equal("200", (await Refund(uuid, dto)).Codigo); Assert.Equal("200", (await Refund(uuid, dto)).Codigo);
        await AssertStock(9, 1);
    }
    [PostgresCheckoutFact] public async Task ReembolsoParcialOMasUnidades_NoCambiaDineroNiInventario()
    {
        var uuid = await CrearPedido(EstadoPedido.Entregado); await Pago(uuid); var dto = Reembolso();
        dto.Monto = 45; Assert.Equal("400", (await Refund(uuid, dto)).Codigo); dto.Monto = 90;
        await using (var db = Context()) dto.ProductosDevueltos.Add(new() { DetalleUuid = (await db.PedidoDetalles.SingleAsync()).Uuid, Cantidad = 3 });
        Assert.Equal("400", (await Refund(uuid, dto)).Codigo); await AssertStock(8, 0);
        await using var verify = Context(); Assert.Equal(EstadoPagoPedido.Pagado, (await verify.Pedidos.SingleAsync()).EstadoPago);
        Assert.Empty(await verify.PedidoReembolsos.ToListAsync());
    }
    [PostgresCheckoutFact] public async Task Legacy_ConciliacionExplicita_RestituyeSinDuplicar()
    {
        var uuid = await CrearPedido();
        await using (var db = Context()) await db.PedidoDetalles.ExecuteUpdateAsync(s => s.SetProperty(x => x.StockDescontado, (int?)null));
        var cancel = await Estado(uuid, EstadoPedido.Rechazado); Assert.True(cancel.Respuesta!.InventarioPendiente); await AssertStock(8, 0);
        var dto = new ConciliarInventarioPedidoDto { VersionPedido = 1, Referencia = "Conteo y checkout histórico verificados" };
        await using (var db = Context()) dto.StockDescontado.Add(new() { DetalleUuid = (await db.PedidoDetalles.SingleAsync()).Uuid, Cantidad = 2 });
        await using (var db = Context()) Assert.Equal("200", (await ServiceComercio(db).ConciliarInventarioAsync(1, uuid, dto)).Codigo);
        await using (var db = Context()) Assert.Equal("409", (await ServiceComercio(db).ConciliarInventarioAsync(1, uuid, dto)).Codigo);
        await AssertStock(10, 1);
    }
    [PostgresCheckoutFact] public async Task InventarioDeshabilitado_NoFabricaStock_IncidenciaVisible_YReintento()
    {
        var uuid = await CrearPedido();
        await using (var db = Context()) await db.ProductosServicios.ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, (int?)null).SetProperty(x => x.ManejaStock, false));
        var cancel = await Estado(uuid, EstadoPedido.Rechazado); Assert.True(cancel.Respuesta!.InventarioPendiente);
        await using (var db = Context()) await db.ProductosServicios.ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, 0).SetProperty(x => x.ManejaStock, true));
        await using (var db = Context())
        {
            var res = await ServiceComercio(db).ConciliarInventarioAsync(1, uuid, new() { VersionPedido = 1, Referencia = "Catálogo reconciliado" });
            Assert.Equal("200", res.Codigo); Assert.False(res.Respuesta!.InventarioPendiente);
        }
        await AssertStock(2, 1);
    }
    [PostgresCheckoutFact] public async Task ComprobanteTardio_NoReabrePedidoCancelado()
    {
        var uuid = await CrearPedido(EstadoPedido.Aprobado);
        await using (var db = Context()) await db.Pedidos.ExecuteUpdateAsync(s => s.SetProperty(x => x.MetodoPago, MetodoPagoPedido.Transferencia));
        await using var late = Context(); var stale = (await new PedidoRepository(late).ObtenerPedidoClienteAsync(uuid, 5))!;
        Assert.Equal("200", (await Estado(uuid, EstadoPedido.Cancelado)).Codigo);
        await Assert.ThrowsAsync<PedidoOperacionException>(() => new PedidoRepository(late).GuardarComprobanteAsync(stale,
            new() { IdPedido = stale.Id, ArchivoUrl = "synthetic.pdf", IdUsuario = 5 }));
        await AssertStock(10, 1); await using var verify = Context(); Assert.Empty(await verify.ComprobantesPago.ToListAsync());
    }
    [PostgresCheckoutFact] public async Task DosComprobantes_ActivaUno_YVersionImpideAprobarUnaVistaAnterior()
    {
        var uuid = await CrearPedido();
        await using (var db = Context()) await db.Pedidos.ExecuteUpdateAsync(s => s.SetProperty(x => x.MetodoPago, MetodoPagoPedido.Transferencia));
        async Task<bool> Upload()
        {
            await using var db = Context(); var repo = new PedidoRepository(db); var p = (await repo.ObtenerPedidoClienteAsync(uuid, 5))!;
            try { await repo.GuardarComprobanteAsync(p, new() { IdPedido = p.Id, ArchivoUrl = Guid.NewGuid() + ".pdf", IdUsuario = 5 }); return true; }
            catch (PedidoOperacionException) { return false; }
        }
        var results = await Task.WhenAll(Upload(), Upload()); Assert.Single(results, x => x);
        Assert.Equal("409", (await Pago(uuid, 0)).Codigo); Assert.Equal("200", (await Pago(uuid, 1)).Codigo);
        await using var verify = Context(); Assert.Equal(1, await verify.ComprobantesPago.CountAsync(x => x.Activo));
    }
    [PostgresCheckoutFact] public async Task FallaTrasRestituirStock_RollbackPagoComisionReembolsoHistorial()
    {
        var uuid = await CrearPedido(); await Pago(uuid); var dto = Reembolso();
        Assert.Equal("500", (await Refund(uuid, dto, new FallarReembolso())).Codigo);
        await AssertStock(8, 0); await using (var db = Context())
        {
            Assert.Equal(EstadoPagoPedido.Pagado, (await db.Pedidos.SingleAsync()).EstadoPago);
            Assert.Equal(1, (await db.Comisiones.SingleAsync()).Estatus); Assert.Empty(await db.PedidoReembolsos.ToListAsync());
        }
        Assert.Equal("200", (await Refund(uuid, dto)).Codigo); await AssertStock(10, 1);
    }
    [PostgresCheckoutFact] public async Task NotificacionFallida_NoOcultaReembolsoConfirmado()
    {
        var uuid = await CrearPedido(); await Pago(uuid); await using var db = Context();
        Assert.Equal("200", (await ServiceComercio(db, fallaNotificacion: true).ReembolsarAsync(1, uuid, Reembolso())).Codigo);
        await AssertStock(10, 1);
    }
    [PostgresCheckoutFact] public async Task ComisionYaLiquidada_ReembolsoGeneraCredito_SinReescribirCobro()
    {
        var uuid = await CrearPedido(); await Pago(uuid);
        await using (var db = Context()) await db.Comisiones.ExecuteUpdateAsync(s => s.SetProperty(x => x.Estatus, 2).SetProperty(x => x.FechaPago, DateTime.UtcNow));
        Assert.Equal("200", (await Refund(uuid, Reembolso())).Codigo);
        await using var verify = Context(); var rows = await verify.Comisiones.OrderBy(x => x.TipoOperacion).ToListAsync();
        Assert.Equal(2, rows.Count); Assert.Equal(2, rows[0].Estatus); Assert.Equal(5m, rows[0].MontoComision);
        Assert.Equal(2, rows[1].TipoOperacion); Assert.Equal(-5m, rows[1].MontoComision); Assert.Equal(1, rows[1].Estatus);
        var saldo = await new PagoComisionRepository(verify).ObtenerSumaComisionesPendientesAsync(1, DateTime.UtcNow.AddYears(1));
        Assert.Equal(-5m, saldo); // Credits survive date/period filters until consumed.
    }
    [PostgresCheckoutFact] public async Task ComisionEnRevision_ReembolsoConservaLoteYGeneraCredito()
    {
        var uuid = await CrearPedido(); await Pago(uuid);
        await using (var db = Context())
        {
            var cuenta = new CuentaBancariaAdLocal { Banco = "Banco sintético", Beneficiario = "AdLocal", Clabe = "000000000000000000" };
            db.CuentasBancariasAdLocal.Add(cuenta); await db.SaveChangesAsync();
            var c = await db.Comisiones.SingleAsync();
            await new PagoComisionRepository(db).CrearPagoAsync(new() { IdComercio = 1, IdCuentaBancariaAdLocal = cuenta.Id,
                Monto = 5, ComprobanteUrl = "synthetic.pdf", Detalles = [new() { IdComision = c.Id, Monto = 5 }] });
        }
        Assert.Equal("200", (await Refund(uuid, Reembolso())).Codigo);
        await using var verify = Context(); Assert.Equal(2, await verify.Comisiones.CountAsync());
        Assert.Equal((int)EstatusComision.Retenida, (await verify.Comisiones.SingleAsync(x => x.TipoOperacion == 2)).Estatus);
        Assert.Equal(0m, await new PagoComisionRepository(verify).ObtenerSumaComisionesPendientesAsync(1, DateTime.UtcNow.AddYears(-1)));
        var pago = await verify.PagosComisiones.SingleAsync();
        var service = new PagoComisionService(new PagoComisionRepository(verify), new PedidoComercioRepository(verify), new Mock<IAmazonS3>().Object, new ConfigurationBuilder().Build());
        Assert.Equal("200", (await service.RevisarPagoAsync(7, pago.Uuid, new() { Aprobar = true })).Codigo);
        Assert.Equal(1, await verify.PagosComisionesDetalle.CountAsync());
        Assert.Equal(-5m, await new PagoComisionRepository(verify).ObtenerSumaComisionesPendientesAsync(1, DateTime.UtcNow.AddYears(1)));
    }
    [PostgresCheckoutFact] public async Task CatalogoDesactualizado_NoSobrescribeStockRestituido()
    {
        var uuid = await CrearPedido(); await using var staleDb = Context(); var stale = await staleDb.ProductosServicios.SingleAsync();
        var version = stale.Version; Assert.NotEqual(0u, version);
        await Estado(uuid, EstadoPedido.Rechazado); stale.Nombre = "Nombre editado antes de la devolución";
        staleDb.ProductosServicios.Update(stale);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleDb.SaveChangesAsync()); await AssertStock(10, 1);
    }
    [PostgresCheckoutFact] public async Task Reembolso_AjenoOColaborador_NoPuedeRegistrar()
    {
        var uuid = await CrearPedido(); await Pago(uuid); await using var db = Context();
        Assert.Equal("403", (await ServiceComercio(db, 7, "Colaborador").ReembolsarAsync(1, uuid, Reembolso())).Codigo);
        Assert.Equal("403", (await ServiceComercio(db, 6).ReembolsarAsync(1, uuid, Reembolso())).Codigo);
        Assert.Equal("404", (await ServiceComercio(db).ReembolsarAsync(1, Guid.NewGuid(), Reembolso())).Codigo);
        await AssertStock(8, 0);
    }
    [PostgresCheckoutFact] public async Task Cliente_VeReferenciaYMotivo_SoloDeSuPedido()
    {
        var uuid = await CrearPedido(); await Pago(uuid); await Refund(uuid, Reembolso()); await using var db = Context();
        var repo = new PedidoRepository(db); var detail = await repo.ObtenerDetallePedidoClienteAsync(uuid, 5);
        Assert.NotNull(detail!.Reembolso); Assert.Equal("TRANSFERENCIA-SINTETICA-001", detail.Reembolso!.Referencia);
        Assert.Contains(detail.Historial, x => x.Evento == "Reembolso" && x.EstadoPago == EstadoPagoPedido.Reembolsado);
        Assert.Null(await repo.ObtenerDetallePedidoClienteAsync(uuid, 6));
    }

    [PostgresCheckoutFact] public async Task Pago_FallaComision_RevierteCobroYAuditoria()
    {
        var uuid = await CrearPedido(); await using (var db = Context(new FallarComision()))
            Assert.Equal("500", (await ServiceComercio(db).RevisarPagoAsync(1, uuid, new() { VersionPedido = 0, EstadoPago = EstadoPagoPedido.Pagado })).Codigo);
        await using var verify = Context(); var p = await verify.Pedidos.SingleAsync();
        Assert.Equal(EstadoPagoPedido.Pendiente, p.EstadoPago); Assert.Equal(0, p.Version);
        Assert.Empty(await verify.Comisiones.ToListAsync()); Assert.Equal(0, await verify.Set<PedidoHistorialEstado>().CountAsync(x => x.Evento == "Pago"));
    }
    [PostgresCheckoutFact] public async Task ConciliacionComisionesTardia_NoCobraPedidoYaReembolsado()
    {
        var uuid = await CrearPedido(); await Pago(uuid); await using var stale = Context();
        var snapshot = await stale.Pedidos.SingleAsync();
        await using (var db = Context()) await db.Comisiones.ExecuteDeleteAsync();
        Assert.Equal("200", (await Refund(uuid, Reembolso())).Codigo);
        await new ComisionService(new ComisionRepository(stale)).RegistrarVentaAsync(snapshot);
        await using var verify = Context(); Assert.Empty(await verify.Comisiones.ToListAsync());
    }
    [PostgresCheckoutFact] public async Task LegacyEntregado_ConciliaAntesDeDeclararDevolucion()
    {
        var uuid = await CrearPedido(EstadoPedido.Entregado); await Pago(uuid); var dto = Reembolso();
        Guid detalle;
        await using (var db = Context())
        {
            detalle = (await db.PedidoDetalles.SingleAsync()).Uuid;
            await db.PedidoDetalles.ExecuteUpdateAsync(s => s.SetProperty(x => x.StockDescontado, (int?)null));
        }
        dto.ProductosDevueltos = [new() { DetalleUuid = detalle, Cantidad = 2 }];
        Assert.Equal("400", (await Refund(uuid, dto)).Codigo);
        await using (var db = Context()) Assert.Equal("200", (await ServiceComercio(db).ConciliarInventarioAsync(1, uuid,
            new() { VersionPedido = 1, Referencia = "Conteo histórico verificado", StockDescontado = [new() { DetalleUuid = detalle, Cantidad = 2 }] })).Codigo);
        await AssertStock(8, 0); dto.VersionPedido = 2;
        Assert.Equal("200", (await Refund(uuid, dto)).Codigo); await AssertStock(10, 1);
    }
    [PostgresCheckoutFact] public async Task MigracionFase3_ConservaPedidosLegacy_SeAplicaDosVeces_SinAlterarXmin()
    {
        await CrearPedido(); await using var db = Context();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE pedido_reembolsos; DROP TABLE pedido_movimientos_inventario; " +
            "ALTER TABLE pedidos DROP COLUMN \"Version\", DROP COLUMN \"RestituirReserva\"; " +
            "ALTER TABLE pedido_detalles DROP COLUMN \"StockDescontado\", DROP COLUMN \"StockPorRestituir\", DROP COLUMN \"StockRestituido\"; " +
            "ALTER TABLE pedido_historial_estados DROP COLUMN \"Evento\", DROP COLUMN \"EstadoPagoAnterior\", DROP COLUMN \"EstadoPagoNuevo\";");
        await db.Database.ExecuteSqlRawAsync(db.GetService<IHistoryRepository>().GetCreateScript());
        var script = db.GetService<IMigrator>().GenerateScript("20261012211320_CheckoutRequestHash", "20261012215314_PedidosInventarioReembolsos", MigrationsSqlGenerationOptions.Idempotent);
        Assert.DoesNotContain("ADD xmin", script); Assert.DoesNotContain("Publicidades", script); Assert.DoesNotContain("Eventos", script);
        await db.Database.ExecuteSqlRawAsync(script); await db.Database.ExecuteSqlRawAsync(script);
        var p = await db.Pedidos.SingleAsync(); Assert.Equal(0, p.Version);
        Assert.Null((await db.PedidoDetalles.SingleAsync()).StockDescontado);
        Assert.Equal("Estado", (await db.Set<PedidoHistorialEstado>().SingleAsync()).Evento);
        await AssertStock(8, 0);
    }
    [PostgresCheckoutFact] public async Task HttpReal_RolesVersionObligatoria_ReembolsoYReintento()
    {
        var uuid = await CrearPedido(); await Pago(uuid); var dto = Reembolso();
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers(o => o.Filters.Add<ValidationFilter>()).AddApplicationPart(typeof(AdLocalAPI.Controllers.PedidosComercioController).Assembly);
        builder.Services.AddValidatorsFromAssemblyContaining<RegistrarReembolsoPedidoDtoValidator>();
        builder.Services.AddHttpContextAccessor(); builder.Services.AddScoped<JwtContext>(); builder.Services.AddScoped(_ => Context());
        builder.Services.AddScoped<IPedidoComercioRepository, PedidoComercioRepository>(); builder.Services.AddScoped<IPedidoComercioService, PedidoComercioService>();
        builder.Services.AddSingleton(new Mock<IAmazonS3>().Object); builder.Services.AddSingleton(new Mock<INotificacionService>().Object);
        builder.Services.AddSingleton(new Mock<IComisionService>().Object);
        const string key = "Phase3SyntheticKeyForHttpContractOnly123456789!";
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters =
            new TokenValidationParameters { ValidateIssuer = false, ValidateAudience = false, ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)) });
        builder.Services.AddAuthorization(); await using var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) }; var url = $"/api/PedidosComercio/{uuid}/reembolso?comercioId=1";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(url, dto)).StatusCode);
        void Login(string rol, string id = "7")
        {
            var token = new JwtSecurityToken(claims: [new Claim("id", id), new Claim("rol", rol), new Claim(ClaimTypes.Role, rol)],
                expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        Login("Colaborador"); Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(url, dto)).StatusCode);
        Login("Comercio", "6"); Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(url, dto)).StatusCode);
        Login("Comercio");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/PedidosComercio/{uuid}/estado?comercioId=1", new { estado = 2 })).StatusCode);
        var first = await client.PostAsJsonAsync(url, dto); Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var retry = await client.PostAsJsonAsync(url, dto); Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var a = await first.Content.ReadFromJsonAsync<ApiResponse<PedidoComercioDetalleDto>>();
        var b = await retry.Content.ReadFromJsonAsync<ApiResponse<PedidoComercioDetalleDto>>();
        Assert.Equal(a!.Respuesta!.Reembolso!.Uuid, b!.Respuesta!.Reembolso!.Uuid); await AssertStock(10, 1); await app.StopAsync();
    }

    [PostgresCheckoutFact] public async Task Preparado_CancelacionSinRecuperar_NoReponeStockConsumido()
    {
        var uuid = await CrearPedido(EstadoPedido.Preparando); await using var db = Context();
        Assert.Equal("400", (await Estado(uuid, EstadoPedido.Cancelado)).Codigo);
        var result = await ServiceComercio(db).CambiarEstadoAsync(1, uuid,
            new() { VersionPedido = 0, Estado = EstadoPedido.Cancelado, Comentario = "Producto consumido o no recuperado", ProductosRestituibles = [] });
        Assert.Equal("200", result.Codigo); Assert.False(result.Respuesta!.RestituirReserva); await AssertStock(8, 0);
        Assert.Equal("409", (await ServiceComercio(db).ConciliarInventarioAsync(1, uuid,
            new() { VersionPedido = 1, Referencia = "Intento de cambiar una política ya registrada", RestituirReserva = true })).Codigo);
    }
    [PostgresCheckoutFact] public async Task Preparado_CancelacionReponeSoloUnidadRecuperada()
    {
        var uuid = await CrearPedido(EstadoPedido.ListoParaRecoger); await using var db = Context(); var detalle = await db.PedidoDetalles.SingleAsync();
        var result = await ServiceComercio(db).CambiarEstadoAsync(1, uuid,
            new() { VersionPedido = 0, Estado = EstadoPedido.Cancelado, Comentario = "Una unidad apta", ProductosRestituibles = [new() { DetalleUuid = detalle.Uuid, Cantidad = 1 }] });
        Assert.Equal("200", result.Codigo); await AssertStock(9, 1);
    }
    [PostgresCheckoutFact] public async Task Enviado_ReembolsoSinRecuperar_NoReponeMercanciaEnTransito()
    {
        var uuid = await CrearPedido(EstadoPedido.Enviado); await Pago(uuid);
        var res = await Refund(uuid, Reembolso()); Assert.Equal("200", res.Codigo);
        Assert.Equal(EstadoPedido.Cancelado, res.Respuesta!.Estado); Assert.False(res.Respuesta.RestituirReserva);
        await AssertStock(8, 0);
    }
    [PostgresCheckoutFact] public async Task LegacyCancelado_SinEvidenciaNoLibera_ConDecisionNoReponeConsumidos()
    {
        var uuid = await CrearPedido(EstadoPedido.Cancelado);
        await using var db = Context(); await db.PedidoDetalles.ExecuteUpdateAsync(s => s.SetProperty(x => x.StockDescontado, (int?)null));
        var dto = new ConciliarInventarioPedidoDto { VersionPedido = 0, Referencia = "Histórico de producto preparado", StockDescontado = [new() { DetalleUuid = (await db.PedidoDetalles.SingleAsync()).Uuid, Cantidad = 2 }] };
        Assert.Equal("400", (await ServiceComercio(db).ConciliarInventarioAsync(1, uuid, dto)).Codigo);
        dto.RestituirReserva = false;
        Assert.Equal("200", (await ServiceComercio(db).ConciliarInventarioAsync(1, uuid, dto)).Codigo); await AssertStock(8, 0);
    }
    [PostgresCheckoutFact] public async Task LoteRechazado_AnulaVentaReembolsadaYCreditoRetenido()
    {
        var uuid = await CrearPedido(); await Pago(uuid);
        Guid lote;
        await using (var db = Context())
        {
            var cuenta = new CuentaBancariaAdLocal { Banco = "Sintético", Beneficiario = "AdLocal", Clabe = "000000000000000000" };
            db.CuentasBancariasAdLocal.Add(cuenta); await db.SaveChangesAsync();
            var c = await db.Comisiones.SingleAsync();
            lote = (await new PagoComisionRepository(db).CrearPagoAsync(new() { IdComercio = 1, IdCuentaBancariaAdLocal = cuenta.Id,
                Monto = 5, ComprobanteUrl = "synthetic.pdf", Detalles = [new() { IdComision = c.Id, Monto = 5 }] })).Uuid;
        }
        await Refund(uuid, Reembolso()); await using var verify = Context();
        var service = new PagoComisionService(new PagoComisionRepository(verify), new PedidoComercioRepository(verify), new Mock<IAmazonS3>().Object, new ConfigurationBuilder().Build());
        Assert.Equal("200", (await service.RevisarPagoAsync(7, lote, new() { Aprobar = false, Comentario = "No se recibió la transferencia" })).Codigo);
        Assert.All(await verify.Comisiones.ToListAsync(), x => Assert.Equal((int)EstatusComision.Cancelada, x.Estatus));
        Assert.Equal(0m, await new PagoComisionRepository(verify).ObtenerSumaComisionesPendientesAsync(1, DateTime.UtcNow.AddYears(-1)));
        Assert.Empty(await verify.PagosComisionesDetalle.ToListAsync()); Assert.Single(await verify.PedidoReembolsos.ToListAsync());
    }

    private sealed class FallarComision : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO comisiones")) throw new IOException("Fallo sintético de comisión");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FallarReembolso : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO pedido_reembolsos")) throw new IOException("Fallo sintético del reembolso después de inventario");
            return ValueTask.FromResult(result);
        }
    }
}
