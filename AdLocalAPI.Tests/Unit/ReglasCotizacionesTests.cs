using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Unit
{
    public class ReglasCotizacionesTests
    {
        private readonly Mock<ICotizacionRepository> _cotizacionRepoMock;
        private readonly CotizacionService _service;

        public ReglasCotizacionesTests()
        {
            _cotizacionRepoMock = new Mock<ICotizacionRepository>();
            _service = new CotizacionService(_cotizacionRepoMock.Object);
        }

        // ==============================================================
        // 1. REGLAS DE CREACIÓN Y VALIDACIÓN DE COTIZACIÓN
        // ==============================================================

        [Fact]
        public async Task CrearCotizacion_DtoNulo_Retorna400BadRequest()
        {
            var resultado = await _service.CrearCotizacionAsync(10, null!);

            Assert.Equal("400", resultado.Codigo);
            Assert.Equal("Datos de cotización inválidos.", resultado.Mensaje);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        public async Task CrearCotizacion_SolicitudVacia_Retorna400BadRequest(string? solicitudInvalida)
        {
            var resultado = await _service.CrearCotizacionAsync(10, new CrearCotizacionDto
            {
                ProductoUuid = Guid.NewGuid(),
                Solicitud = solicitudInvalida!
            });

            Assert.Equal("400", resultado.Codigo);
            Assert.Equal("Describe lo que necesitas cotizar.", resultado.Mensaje);
        }

        [Fact]
        public async Task CrearCotizacion_ServicioInexistente_Retorna404NotFound()
        {
            var prodUuid = Guid.NewGuid();
            _cotizacionRepoMock
                .Setup(r => r.ObtenerServicioParaCotizarAsync(prodUuid, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductosServicios?)null);

            var resultado = await _service.CrearCotizacionAsync(10, new CrearCotizacionDto
            {
                ProductoUuid = prodUuid,
                Solicitud = "Cotización para banquete de 50 personas"
            });

            Assert.Equal("404", resultado.Codigo);
            Assert.Equal("Servicio no encontrado o no disponible para cotización.", resultado.Mensaje);
        }

        [Fact]
        public async Task CrearCotizacion_DatosValidos_CreaCotizacionEnEstadoPendiente()
        {
            var prodUuid = Guid.NewGuid();
            const long idUsuario = 15;
            const long idComercio = 2;
            const long idServicio = 77;

            _cotizacionRepoMock
                .Setup(r => r.ObtenerServicioParaCotizarAsync(prodUuid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProductosServicios
                {
                    Id = idServicio,
                    Uuid = prodUuid,
                    IdComercio = idComercio,
                    Nombre = "Servicio de Limpieza Industrial"
                });

            Cotizacion? guardada = null;
            _cotizacionRepoMock
                .Setup(r => r.CrearAsync(It.IsAny<Cotizacion>(), It.IsAny<CancellationToken>()))
                .Callback<Cotizacion, CancellationToken>((c, _) => guardada = c)
                .ReturnsAsync((Cotizacion c, CancellationToken _) => c);

            var resultado = await _service.CrearCotizacionAsync(idUsuario, new CrearCotizacionDto
            {
                ProductoUuid = prodUuid,
                Solicitud = "Requiero limpieza de nave de 1000m2 para la próxima semana."
            });

            Assert.Equal("200", resultado.Codigo);
            Assert.NotNull(guardada);
            Assert.Equal(idUsuario, guardada!.IdUsuario);
            Assert.Equal(idComercio, guardada.IdComercio);
            Assert.Equal(idServicio, guardada.IdProductoServicio);
            Assert.Equal(EstadoCotizacion.Pendiente, guardada.Estado);
            Assert.Equal("Requiero limpieza de nave de 1000m2 para la próxima semana.", guardada.Solicitud);
            Assert.True(guardada.FechaCreacion <= DateTime.UtcNow);

            _cotizacionRepoMock.Verify(r => r.CrearAsync(It.IsAny<Cotizacion>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // ==============================================================
        // 2. REGLAS DE CANCELACIÓN DE COTIZACIÓN
        // ==============================================================

        [Fact]
        public async Task CancelarCotizacion_CotizacionInexistente_Retorna404()
        {
            var uuid = Guid.NewGuid();
            _cotizacionRepoMock
                .Setup(r => r.ObtenerPorUuidYUsuarioAsync(uuid, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Cotizacion?)null);

            var resultado = await _service.CancelarCotizacionAsync(10, uuid);

            Assert.Equal("404", resultado.Codigo);
            Assert.Equal("Cotización no encontrada.", resultado.Mensaje);
        }

        [Theory]
        [InlineData(EstadoCotizacion.Aceptada)]
        [InlineData(EstadoCotizacion.Cancelada)]
        public async Task CancelarCotizacion_EnEstadoAceptadaOCancelada_Retorna409Conflict(EstadoCotizacion estadoInmutable)
        {
            var uuid = Guid.NewGuid();
            _cotizacionRepoMock
                .Setup(r => r.ObtenerPorUuidYUsuarioAsync(uuid, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Cotizacion
                {
                    Id = 5,
                    Uuid = uuid,
                    IdUsuario = 10,
                    Estado = estadoInmutable
                });

            var resultado = await _service.CancelarCotizacionAsync(10, uuid);

            Assert.Equal("409", resultado.Codigo);
            Assert.Equal("Ya no se puede cancelar la cotización.", resultado.Mensaje);
            _cotizacionRepoMock.Verify(r => r.ActualizarAsync(It.IsAny<Cotizacion>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(EstadoCotizacion.Pendiente)]
        [InlineData(EstadoCotizacion.Respondida)]
        public async Task CancelarCotizacion_EnEstadoPendienteORespondida_PermiteCancelar(EstadoCotizacion estadoCancelable)
        {
            var uuid = Guid.NewGuid();
            var cotizacion = new Cotizacion
            {
                Id = 8,
                Uuid = uuid,
                IdUsuario = 10,
                Estado = estadoCancelable
            };

            _cotizacionRepoMock
                .Setup(r => r.ObtenerPorUuidYUsuarioAsync(uuid, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(cotizacion);

            var resultado = await _service.CancelarCotizacionAsync(10, uuid);

            Assert.Equal("200", resultado.Codigo);
            Assert.Equal(EstadoCotizacion.Cancelada, cotizacion.Estado);
            Assert.NotNull(cotizacion.FechaActualizacion);
            _cotizacionRepoMock.Verify(r => r.ActualizarAsync(cotizacion, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ==============================================================
        // 3. REGLAS DE LISTADO DE COTIZACIONES DE USUARIO
        // ==============================================================

        [Fact]
        public async Task ObtenerMiasAsync_RetornaListaDeCotizacionesDelUsuario()
        {
            const long idUsuario = 12;
            var items = new List<CotizacionItemDto>
            {
                new() { Uuid = Guid.NewGuid(), Servicio = "Mantenimiento AC", Estado = EstadoCotizacion.Pendiente },
                new() { Uuid = Guid.NewGuid(), Servicio = "Pintura General", Estado = EstadoCotizacion.Respondida }
            };

            _cotizacionRepoMock
                .Setup(r => r.ObtenerMiasAsync(idUsuario, It.IsAny<CancellationToken>()))
                .ReturnsAsync(items);

            var resultado = await _service.ObtenerMiasAsync(idUsuario);

            Assert.Equal("200", resultado.Codigo);
            Assert.NotNull(resultado.Respuesta);
            Assert.Equal(2, resultado.Respuesta.Count);
        }
    }
}
