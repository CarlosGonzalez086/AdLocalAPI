using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Utils;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Unit
{
    public class ReglasComisionesTests
    {
        private readonly Mock<IComisionRepository> _comisionRepoMock;
        private readonly ComisionService _service;

        public ReglasComisionesTests()
        {
            _comisionRepoMock = new Mock<IComisionRepository>();
            _service = new ComisionService(_comisionRepoMock.Object);
        }

        // ==============================================================
        // 1. REGLAS DE REGISTRO DE VENTA Y RETENCIÓN DE COMISIONES
        // ==============================================================

        [Theory]
        [InlineData(EstadoPagoPedido.Pendiente)]
        [InlineData(EstadoPagoPedido.PendienteVerificacion)]
        [InlineData(EstadoPagoPedido.Rechazado)]
        [InlineData(EstadoPagoPedido.Reembolsado)]
        public async Task RegistrarVenta_PedidoNoPagado_NoGeneraComision(EstadoPagoPedido estadoNoPagado)
        {
            var pedido = new Pedido
            {
                Id = 1,
                IdComercio = 10,
                Total = 500m,
                MontoComision = 25m,
                PorcentajeComision = 5m,
                EstadoPago = estadoNoPagado
            };

            await _service.RegistrarVentaAsync(pedido);

            _comisionRepoMock.Verify(r => r.AgregarAsync(It.IsAny<Comision>()), Times.Never);
        }

        [Fact]
        public async Task RegistrarVenta_MontoComisionCeroOMenor_NoGeneraComision()
        {
            var pedido = new Pedido
            {
                Id = 2,
                IdComercio = 10,
                Total = 100m,
                MontoComision = 0m,
                PorcentajeComision = 0m,
                EstadoPago = EstadoPagoPedido.Pagado
            };

            await _service.RegistrarVentaAsync(pedido);

            _comisionRepoMock.Verify(r => r.AgregarAsync(It.IsAny<Comision>()), Times.Never);
        }

        [Fact]
        public async Task RegistrarVenta_OperacionYaExiste_IdempotenciaNoDuplicaComision()
        {
            var pedido = new Pedido
            {
                Id = 3,
                IdComercio = 10,
                Total = 300m,
                MontoComision = 15m,
                PorcentajeComision = 5m,
                EstadoPago = EstadoPagoPedido.Pagado
            };

            _comisionRepoMock
                .Setup(r => r.ExisteOperacionAsync((int)TipoOperacionComision.Venta, pedido.Id))
                .ReturnsAsync(true);

            await _service.RegistrarVentaAsync(pedido);

            _comisionRepoMock.Verify(r => r.AgregarAsync(It.IsAny<Comision>()), Times.Never);
        }

        [Fact]
        public async Task RegistrarVenta_VentaPagadaValida_RegistraComisionCorrectamente()
        {
            var pedido = new Pedido
            {
                Id = 4,
                NumeroPedido = "PED-2026-004",
                IdComercio = 20,
                Total = 400.00m,
                Subtotal = 350.00m,
                PorcentajeComision = 5.0m,
                ComisionFija = 2.50m,
                MontoComision = 20.00m,
                EstadoPago = EstadoPagoPedido.Pagado
            };

            _comisionRepoMock
                .Setup(r => r.ExisteOperacionAsync((int)TipoOperacionComision.Venta, pedido.Id))
                .ReturnsAsync(false);

            await _service.RegistrarVentaAsync(pedido);

            _comisionRepoMock.Verify(r => r.AgregarAsync(It.Is<Comision>(c =>
                c.IdComercio == 20 &&
                c.TipoOperacion == (int)TipoOperacionComision.Venta &&
                c.IdReferencia == pedido.Id &&
                c.MontoOperacion == 400.00m &&
                c.PorcentajeComision == 5.0m &&
                c.MontoComision == 20.00m &&
                c.Estatus == (int)EstatusComision.Pendiente &&
                c.Activo
            )), Times.Once);
        }

        // ==============================================================
        // 2. REGLAS DE LIQUIDACIÓN DE COMISIONES
        // ==============================================================

        [Fact]
        public async Task LiquidarAsync_SinComisionesPendientes_Retorna404()
        {
            const long comercioId = 5;
            _comisionRepoMock
                .Setup(r => r.ObtenerPendientesParaLiquidacionAsync(comercioId, It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Comision>());

            var resultado = await _service.LiquidarAsync(comercioId, "mes");

            Assert.Equal("404", resultado.Codigo);
            Assert.Equal("No hay comisiones pendientes en el periodo.", resultado.Mensaje);
            _comisionRepoMock.Verify(r => r.ActualizarComisionesAsync(It.IsAny<List<Comision>>()), Times.Never);
        }

        [Fact]
        public async Task LiquidarAsync_ConComisionesPendientes_MarcaTodasComoPagadasYActualiza()
        {
            const long comercioId = 5;
            var pendientes = new List<Comision>
            {
                new() { Id = 1, IdComercio = comercioId, MontoComision = 15.50m, Estatus = (int)EstatusComision.Pendiente },
                new() { Id = 2, IdComercio = comercioId, MontoComision = 24.50m, Estatus = (int)EstatusComision.Pendiente },
                new() { Id = 3, IdComercio = comercioId, MontoComision = 10.00m, Estatus = (int)EstatusComision.Pendiente }
            };

            _comisionRepoMock
                .Setup(r => r.ObtenerPendientesParaLiquidacionAsync(comercioId, It.IsAny<DateTime>()))
                .ReturnsAsync(pendientes);

            var resultado = await _service.LiquidarAsync(comercioId, "semana");

            Assert.Equal("200", resultado.Codigo);
            Assert.All(pendientes, c =>
            {
                Assert.Equal((int)EstatusComision.Pagada, c.Estatus);
                Assert.NotNull(c.FechaPago);
            });

            _comisionRepoMock.Verify(r => r.ActualizarComisionesAsync(pendientes), Times.Once);
        }

        [Fact]
        public async Task ObtenerDashboardAsync_CalculaMetricasGlobalesCorrectamente()
        {
            _comisionRepoMock
                .Setup(r => r.ObtenerPedidosPendientesConciliacionAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Pedido>());

            _comisionRepoMock
                .Setup(r => r.ObtenerComisionesPorDiaAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ComisionDiaDto>
                {
                    new() { Fecha = new DateTime(2026, 9, 22), Monto = 120m },
                    new() { Fecha = new DateTime(2026, 9, 23), Monto = 180m }
                });

            _comisionRepoMock.Setup(r => r.ObtenerComisionesMesAsync(It.IsAny<DateTime>())).ReturnsAsync(1500m);
            _comisionRepoMock.Setup(r => r.ObtenerPendienteCobroAsync()).ReturnsAsync(450m);
            _comisionRepoMock.Setup(r => r.ObtenerCobradoMesAsync(It.IsAny<DateTime>())).ReturnsAsync(1050m);

            var resultado = await _service.ObtenerDashboardAsync();

            Assert.Equal("200", resultado.Codigo);
            Assert.NotNull(resultado.Respuesta);
            Assert.Equal(300m, resultado.Respuesta.ComisionesSemana);
            Assert.Equal(1500m, resultado.Respuesta.ComisionesMes);
            Assert.Equal(450m, resultado.Respuesta.PendienteCobro);
            Assert.Equal(1050m, resultado.Respuesta.CobradoMes);
        }
    }
}
