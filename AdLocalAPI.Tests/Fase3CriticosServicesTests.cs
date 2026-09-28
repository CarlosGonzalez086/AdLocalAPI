using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Utils;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class Fase3CriticosServicesTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        // ======================================================
        // PRUEBAS DE COTIZACIONES
        // ======================================================
        [Fact]
        public async Task CotizacionService_CrearCotizacionAsync_ConDatosInvalidos_Retorna400()
        {
            using var context = CreateDbContext();
            var repo = new CotizacionRepository(context);
            var service = new CotizacionService(repo);

            var resultNull = await service.CrearCotizacionAsync(1, null!);
            Assert.Equal("400", resultNull.Codigo);

            var resultEmpty = await service.CrearCotizacionAsync(1, new CrearCotizacionDto { Solicitud = "   " });
            Assert.Equal("400", resultEmpty.Codigo);
        }

        [Fact]
        public async Task CotizacionService_CrearCotizacionAsync_ServicioInexistente_Retorna404()
        {
            using var context = CreateDbContext();
            var repo = new CotizacionRepository(context);
            var service = new CotizacionService(repo);

            var result = await service.CrearCotizacionAsync(1, new CrearCotizacionDto
            {
                ProductoUuid = Guid.NewGuid(),
                Solicitud = "Quiero cotizar un servicio de catering"
            });

            Assert.Equal("404", result.Codigo);
        }

        [Fact]
        public async Task CotizacionService_CrearCotizacionAsync_Exitoso_CreaCotizacion()
        {
            using var context = CreateDbContext();
            var servicioUuid = Guid.NewGuid();

            context.ProductosServicios.Add(new ProductosServicios
            {
                Id = 50,
                Uuid = servicioUuid,
                Nombre = "Servicio Catering",
                IdComercio = 10,
                Modalidad = ModalidadProductoServicio.Cotizacion,
                Activo = true,
                Visible = true
            });
            await context.SaveChangesAsync();

            var repo = new CotizacionRepository(context);
            var service = new CotizacionService(repo);

            var result = await service.CrearCotizacionAsync(1, new CrearCotizacionDto
            {
                ProductoUuid = servicioUuid,
                Solicitud = "Cotización para 50 personas"
            });

            Assert.Equal("200", result.Codigo);
            var creada = await context.Cotizaciones.FirstOrDefaultAsync();
            Assert.NotNull(creada);
            Assert.Equal("Cotización para 50 personas", creada.Solicitud);
            Assert.Equal(EstadoCotizacion.Pendiente, creada.Estado);
            Assert.Equal(10, creada.IdComercio);
        }

        [Fact]
        public async Task CotizacionService_CancelarCotizacionAsync_CambiaEstadoACancelada()
        {
            using var context = CreateDbContext();
            var cotizacionUuid = Guid.NewGuid();

            context.Cotizaciones.Add(new Cotizacion
            {
                Id = 1,
                Uuid = cotizacionUuid,
                IdUsuario = 100,
                IdComercio = 10,
                IdProductoServicio = 50,
                Solicitud = "Cotización de prueba",
                Estado = EstadoCotizacion.Pendiente,
                FechaCreacion = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var repo = new CotizacionRepository(context);
            var service = new CotizacionService(repo);

            var result = await service.CancelarCotizacionAsync(100, cotizacionUuid);
            Assert.Equal("200", result.Codigo);

            var cotizacion = await context.Cotizaciones.FirstAsync(x => x.Uuid == cotizacionUuid);
            Assert.Equal(EstadoCotizacion.Cancelada, cotizacion.Estado);

            // Reintentar cancelar una ya cancelada -> 409
            var resultConflict = await service.CancelarCotizacionAsync(100, cotizacionUuid);
            Assert.Equal("409", resultConflict.Codigo);
        }

        // ======================================================
        // PRUEBAS DE CUENTAS BANCARIAS ADLOCAL
        // ======================================================
        [Fact]
        public async Task CuentaBancariaAdLocalService_CrearAsync_ValidacionesCampos()
        {
            using var context = CreateDbContext();
            var repo = new CuentaBancariaAdLocalRepository(context);
            var service = new CuentaBancariaAdLocalService(repo);

            var resSinBanco = await service.CrearAsync(new GuardarCuentaBancariaAdLocalDto { Banco = "", Beneficiario = "AdLocal" });
            Assert.Equal("400", resSinBanco.Codigo);

            var resSinBeneficiario = await service.CrearAsync(new GuardarCuentaBancariaAdLocalDto { Banco = "BBVA", Beneficiario = "" });
            Assert.Equal("400", resSinBeneficiario.Codigo);

            var resSinCuentas = await service.CrearAsync(new GuardarCuentaBancariaAdLocalDto { Banco = "BBVA", Beneficiario = "AdLocal" });
            Assert.Equal("400", resSinCuentas.Codigo);
        }

        [Fact]
        public async Task CuentaBancariaAdLocalService_CrearAsync_ComoPrincipal_DesmarcaOtras()
        {
            using var context = CreateDbContext();
            context.CuentasBancariasAdLocal.Add(new CuentaBancariaAdLocal
            {
                Id = 1,
                Banco = "Banamex",
                Beneficiario = "AdLocal SA",
                Clabe = "123456789012345678",
                Principal = true,
                Activo = true
            });
            await context.SaveChangesAsync();

            var repo = new CuentaBancariaAdLocalRepository(context);
            var service = new CuentaBancariaAdLocalService(repo);

            var res = await service.CrearAsync(new GuardarCuentaBancariaAdLocalDto
            {
                Banco = "BBVA",
                Beneficiario = "AdLocal Nueva",
                Clabe = "987654321098765432",
                Principal = true
            });

            Assert.Equal("200", res.Codigo);
            Assert.True(res.Respuesta!.Principal);

            var anterior = await context.CuentasBancariasAdLocal.FindAsync((long)1);
            Assert.False(anterior!.Principal);
        }

        [Fact]
        public async Task CuentaBancariaAdLocalService_CambiarEstadoAsync_TogglesActivoYDesmarcaPrincipal()
        {
            using var context = CreateDbContext();
            var uuid = Guid.NewGuid();
            context.CuentasBancariasAdLocal.Add(new CuentaBancariaAdLocal
            {
                Id = 1,
                Uuid = uuid,
                Banco = "BBVA",
                Beneficiario = "AdLocal",
                Clabe = "123456789012345678",
                Principal = true,
                Activo = true
            });
            await context.SaveChangesAsync();

            var repo = new CuentaBancariaAdLocalRepository(context);
            var service = new CuentaBancariaAdLocalService(repo);

            var res = await service.CambiarEstadoAsync(uuid);
            Assert.Equal("200", res.Codigo);

            var cuenta = await context.CuentasBancariasAdLocal.FirstAsync(x => x.Uuid == uuid);
            Assert.False(cuenta.Activo);
            Assert.False(cuenta.Principal);
        }

        // ======================================================
        // PRUEBAS DE PAGOS DE COMISIONES
        // ======================================================
        [Fact]
        public async Task PagoComisionService_ObtenerEstadoAsync_SinPermisos_Retorna403()
        {
            using var context = CreateDbContext();
            var repo = new PagoComisionRepository(context);
            var mockPedidos = new Mock<IPedidoComercioRepository>();
            mockPedidos.Setup(x => x.PuedeGestionarAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long>()))
                .ReturnsAsync(false);

            var mockS3 = new Mock<IAmazonS3>();
            var mockConfig = new Mock<IConfiguration>();

            var service = new PagoComisionService(repo, mockPedidos.Object, mockS3.Object, mockConfig.Object);

            var res = await service.ObtenerEstadoAsync(999, "Comercio", 10);
            Assert.Equal("403", res.Codigo);
        }

        [Fact]
        public async Task PagoComisionService_CrearPagoAsync_MetodoInvalido_Retorna400()
        {
            using var context = CreateDbContext();
            var repo = new PagoComisionRepository(context);
            var mockPedidos = new Mock<IPedidoComercioRepository>();
            mockPedidos.Setup(x => x.PuedeGestionarAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long>()))
                .ReturnsAsync(true);

            var mockS3 = new Mock<IAmazonS3>();
            var mockConfig = new Mock<IConfiguration>();

            var service = new PagoComisionService(repo, mockPedidos.Object, mockS3.Object, mockConfig.Object);

            var res = await service.CrearPagoAsync(1, "Comercio", new CrearPagoComisionDto
            {
                ComercioId = 10,
                MetodoPago = "bitcoin"
            });

            Assert.Equal("400", res.Codigo);
        }

        [Fact]
        public async Task PagoComisionService_RevisarPagoAsync_Aprobar_CambiaEstadoYComisionesAPagadas()
        {
            using var context = CreateDbContext();
            var pagoUuid = Guid.NewGuid();

            var comision = new Comision
            {
                Id = 101,
                IdComercio = 10,
                MontoComision = 150.00m,
                Estatus = 1,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            context.Comisiones.Add(comision);

            var pago = new PagoComision
            {
                Id = 1,
                Uuid = pagoUuid,
                IdComercio = 10,
                IdCuentaBancariaAdLocal = 5,
                Periodo = "semana",
                MetodoPago = "transferencia",
                Monto = 150.00m,
                ComprobanteUrl = "test.png",
                Estatus = 1,
                IdUsuarioCreacion = 1,
                Detalles = new List<PagoComisionDetalle>
                {
                    new() { Id = 1, IdPagoComision = 1, IdComision = 101, Monto = 150.00m }
                }
            };
            context.PagosComisiones.Add(pago);
            await context.SaveChangesAsync();

            var repo = new PagoComisionRepository(context);
            var mockPedidos = new Mock<IPedidoComercioRepository>();
            var mockS3 = new Mock<IAmazonS3>();
            var mockConfig = new Mock<IConfiguration>();

            var service = new PagoComisionService(repo, mockPedidos.Object, mockS3.Object, mockConfig.Object);

            var res = await service.RevisarPagoAsync(99, pagoUuid, new RevisarPagoComisionDto
            {
                Aprobar = true,
                Comentario = "Comprobante verificado"
            });

            Assert.Equal("200", res.Codigo);

            var pagoDb = await context.PagosComisiones.FindAsync((long)1);
            Assert.Equal(2, pagoDb!.Estatus); // 2 = Aprobado
            Assert.Equal(99, pagoDb.IdUsuarioRevision);
            Assert.NotNull(pagoDb.FechaRevision);

            var comisionDb = await context.Comisiones.FindAsync((long)101);
            Assert.Equal((int)EstatusComision.Pagada, comisionDb!.Estatus);
            Assert.NotNull(comisionDb.FechaPago);
        }

        [Fact]
        public async Task PagoComisionService_RevisarPagoAsync_Rechazar_EliminaDetallesParaReprocesar()
        {
            using var context = CreateDbContext();
            var pagoUuid = Guid.NewGuid();

            var comision = new Comision
            {
                Id = 202,
                IdComercio = 10,
                MontoComision = 200.00m,
                Estatus = 1,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            context.Comisiones.Add(comision);

            var pago = new PagoComision
            {
                Id = 2,
                Uuid = pagoUuid,
                IdComercio = 10,
                IdCuentaBancariaAdLocal = 5,
                Periodo = "semana",
                MetodoPago = "transferencia",
                Monto = 200.00m,
                ComprobanteUrl = "test.png",
                Estatus = 1,
                IdUsuarioCreacion = 1,
                Detalles = new List<PagoComisionDetalle>
                {
                    new() { Id = 2, IdPagoComision = 2, IdComision = 202, Monto = 200.00m }
                }
            };
            context.PagosComisiones.Add(pago);
            await context.SaveChangesAsync();

            var repo = new PagoComisionRepository(context);
            var mockPedidos = new Mock<IPedidoComercioRepository>();
            var mockS3 = new Mock<IAmazonS3>();
            var mockConfig = new Mock<IConfiguration>();

            var service = new PagoComisionService(repo, mockPedidos.Object, mockS3.Object, mockConfig.Object);

            var res = await service.RevisarPagoAsync(99, pagoUuid, new RevisarPagoComisionDto
            {
                Aprobar = false,
                Comentario = "Comprobante ilegible"
            });

            Assert.Equal("200", res.Codigo);

            var pagoDb = await context.PagosComisiones.FindAsync((long)2);
            Assert.Equal(3, pagoDb!.Estatus); // 3 = Rechazado

            // Los detalles deben haber sido eliminados para liberar la comisión
            var detallesCount = await context.PagosComisionesDetalle.CountAsync(x => x.IdPagoComision == 2);
            Assert.Equal(0, detallesCount);

            var comisionDb = await context.Comisiones.FindAsync((long)202);
            Assert.Equal(1, comisionDb!.Estatus); // Sigue pendiente
        }
    }
}
