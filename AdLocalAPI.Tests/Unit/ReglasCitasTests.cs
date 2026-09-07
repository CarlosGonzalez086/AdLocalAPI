using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Unit
{
    public class ReglasCitasTests
    {
        private readonly Mock<ICitaRepository> _citaRepoMock;
        private readonly Mock<IProductosServiciosRepository> _prodServRepoMock;
        private readonly Mock<IHorarioComercioRepository> _horarioComercioRepoMock;
        private readonly Mock<IHorarioCitaServicioRepository> _horarioCitaRepoMock;
        private readonly Mock<IComercioRepository> _comercioRepoMock;

        public ReglasCitasTests()
        {
            _citaRepoMock = new Mock<ICitaRepository>();
            _prodServRepoMock = new Mock<IProductosServiciosRepository>();
            _horarioComercioRepoMock = new Mock<IHorarioComercioRepository>();
            _horarioCitaRepoMock = new Mock<IHorarioCitaServicioRepository>();
            _horarioCitaRepoMock.Setup(r => r.ObtenerPorServicioFechaAsync(It.IsAny<long>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new List<HorarioCitaServicio>());
            _horarioCitaRepoMock.Setup(r => r.ObtenerDisponiblesAsync(It.IsAny<long>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(new List<HorarioCitaServicio>());
            _comercioRepoMock = new Mock<IComercioRepository>();
            _comercioRepoMock.Setup(x => x.PuedeAdministrarAsync(It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(true);
        }

        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private CitaService CrearServicio(AppDbContext context, long userId = 10, string role = "Cliente")
        {
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim("id", userId.ToString()),
                new Claim("rol", role),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
            var jwt = new JwtContext(new HttpContextAccessor { HttpContext = httpContext });

            return new CitaService(
                _citaRepoMock.Object,
                _prodServRepoMock.Object,
                _horarioComercioRepoMock.Object,
                _horarioCitaRepoMock.Object,
                _comercioRepoMock.Object,
                jwt
            );
        }

        // ==============================================================
        // 1. REGLAS DE CREACIÓN Y VALIDACIÓN DE CITAS
        // ==============================================================

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CrearCita_SinNombrePersona_Retorna400(string? nombreInvalido)
        {
            using var context = CreateDbContext();
            var service = CrearServicio(context);

            var resultado = await service.CrearAsync(new CrearCitaDto
            {
                NombrePersona = nombreInvalido!,
                ProductoUuid = Guid.NewGuid(),
                FechaInicio = DateTime.UtcNow.AddDays(1)
            });

            Assert.Equal("400", resultado.Codigo);
            Assert.Equal("Indica el nombre de la persona que recibirá la atención.", resultado.Mensaje);
        }

        [Fact]
        public async Task CrearCita_ServicioNoEncontrado_Retorna404()
        {
            using var context = CreateDbContext();
            var service = CrearServicio(context);
            var prodUuid = Guid.NewGuid();

            _prodServRepoMock
                .Setup(r => r.ObtenerReservablePorUuidAsync(prodUuid))
                .ReturnsAsync((ProductosServicios?)null);

            var resultado = await service.CrearAsync(new CrearCitaDto
            {
                NombrePersona = "Carlos Gómez",
                ProductoUuid = prodUuid,
                FechaInicio = DateTime.UtcNow.AddDays(1)
            });

            Assert.Equal("404", resultado.Codigo);
            Assert.Equal("Servicio no encontrado.", resultado.Mensaje);
        }

        [Fact]
        public async Task CrearCita_HorarioNoDisponible_Retorna409Conflict()
        {
            using var context = CreateDbContext();
            var service = CrearServicio(context);
            var prodUuid = Guid.NewGuid();
            var fecha = new DateTime(2026, 10, 15, 10, 0, 0);

            _prodServRepoMock
                .Setup(r => r.ObtenerReservablePorUuidAsync(prodUuid))
                .ReturnsAsync(new ProductosServicios
                {
                    Id = 1,
                    Uuid = prodUuid,
                    IdComercio = 10,
                    DuracionMinutos = 30
                });

            // No hay horario configurado o está cerrado ese día
            _horarioComercioRepoMock
                .Setup(r => r.ObtenerAsync(10, fecha.DayOfWeek))
                .ReturnsAsync((HorarioComercio?)null);

            var resultado = await service.CrearAsync(new CrearCitaDto
            {
                NombrePersona = "Ana Martínez",
                ProductoUuid = prodUuid,
                FechaInicio = fecha
            });

            Assert.Equal("409", resultado.Codigo);
            Assert.Equal("El horario seleccionado ya no está disponible.", resultado.Mensaje);
        }

        [Fact]
        public async Task CrearCita_EspacioDisponible_ReservaEspacioYGuardaCita()
        {
            using var context = CreateDbContext();
            var service = CrearServicio(context, userId: 10);
            var prodUuid = Guid.NewGuid();
            var fechaInicio = DateTime.Today.AddDays(7).AddHours(10);
            var dateOnly = DateOnly.FromDateTime(fechaInicio);

            var servicio = new ProductosServicios
            {
                Id = 1,
                Uuid = prodUuid,
                IdComercio = 10,
                DuracionMinutos = 45
            };

            var espacio = new HorarioCitaServicio
            {
                Id = 100,
                IdProductoServicio = 1,
                Fecha = dateOnly,
                HoraInicio = fechaInicio.TimeOfDay,
                HoraFin = fechaInicio.TimeOfDay.Add(TimeSpan.FromMinutes(45)),
                Disponible = true,
                IdCita = null
            };

            _prodServRepoMock.Setup(r => r.ObtenerReservablePorUuidAsync(prodUuid)).ReturnsAsync(servicio);
            _horarioComercioRepoMock.Setup(r => r.ObtenerAsync(10, fechaInicio.DayOfWeek)).ReturnsAsync(new HorarioComercio
            {
                Abierto = true,
                HoraApertura = new TimeSpan(8, 0, 0),
                HoraCierre = new TimeSpan(20, 0, 0)
            });
            _citaRepoMock.Setup(r => r.ObtenerOcupadasAsync(10, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Cita>());

            _horarioCitaRepoMock.Setup(r => r.ObtenerDisponiblesAsync(1, dateOnly))
                .ReturnsAsync(new List<HorarioCitaServicio> { espacio });

            _horarioCitaRepoMock.Setup(r => r.ObtenerDisponibleAsync(1, dateOnly, fechaInicio.TimeOfDay))
                .ReturnsAsync(espacio);

            Cita? citaCreada = null;
            _citaRepoMock.Setup(r => r.CrearAsync(It.IsAny<Cita>()))
                .Callback<Cita>(c => { c.Id = 77; citaCreada = c; })
                .ReturnsAsync((Cita c) => { c.Id = 77; return c; });

            _citaRepoMock.Setup(r => r.ObtenerDtoAsync(77)).ReturnsAsync(new CitaDto
            {
                Uuid = Guid.NewGuid(),
                NombrePersona = "Pedro Infante",
                FechaInicio = fechaInicio,
                Estado = EstadoCita.Pendiente
            });

            var resultado = await service.CrearAsync(new CrearCitaDto
            {
                NombrePersona = "Pedro Infante",
                ProductoUuid = prodUuid,
                FechaInicio = fechaInicio
            });

            Assert.Equal("200", resultado.Codigo);
            Assert.NotNull(citaCreada);
            Assert.Equal(10, citaCreada!.IdUsuario);
            Assert.Equal(fechaInicio.AddMinutes(45), citaCreada.FechaFin);
            Assert.False(espacio.Disponible);
            Assert.Equal(77, espacio.IdCita);
            _horarioCitaRepoMock.Verify(r => r.GuardarCambiosAsync(), Times.AtLeastOnce());
        }

        // ==============================================================
        // 2. REGLAS DE CANCELACIÓN DE CITAS
        // ==============================================================

        [Theory]
        [InlineData(EstadoCita.Completada)]
        [InlineData(EstadoCita.Cancelada)]
        [InlineData(EstadoCita.NoAsistio)]
        public async Task CancelarCliente_CitaEnEstadoTerminal_Retorna409Conflict(EstadoCita estadoTerminal)
        {
            using var context = CreateDbContext();
            var service = CrearServicio(context, userId: 10);
            var citaUuid = Guid.NewGuid();

            _citaRepoMock.Setup(r => r.ObtenerPorUuidClienteAsync(citaUuid, 10))
                .ReturnsAsync(new Cita
                {
                    Id = 50,
                    Uuid = citaUuid,
                    IdUsuario = 10,
                    Estado = estadoTerminal
                });

            var resultado = await service.CancelarClienteAsync(citaUuid, "Ya no puedo asistir");

            Assert.Equal("409", resultado.Codigo);
            Assert.Equal("Esta cita ya no se puede cancelar.", resultado.Mensaje);
        }

        [Fact]
        public async Task CancelarCliente_CitaPendiente_CancelaYLiberaEspacioHorario()
        {
            using var context = CreateDbContext();
            var service = CrearServicio(context, userId: 10);
            var citaUuid = Guid.NewGuid();

            var cita = new Cita
            {
                Id = 50,
                Uuid = citaUuid,
                IdUsuario = 10,
                Estado = EstadoCita.Pendiente
            };

            var espacio = new HorarioCitaServicio
            {
                Id = 200,
                IdCita = 50,
                Disponible = false
            };

            _citaRepoMock.Setup(r => r.ObtenerPorUuidClienteAsync(citaUuid, 10)).ReturnsAsync(cita);
            _horarioCitaRepoMock.Setup(r => r.ObtenerPorCitaAsync(50)).ReturnsAsync(espacio);
            _citaRepoMock.Setup(r => r.ObtenerDtoAsync(50)).ReturnsAsync(new CitaDto
            {
                Uuid = citaUuid,
                Estado = EstadoCita.Cancelada
            });

            var resultado = await service.CancelarClienteAsync(citaUuid, "Cambio de planes");

            Assert.Equal("200", resultado.Codigo);
            Assert.Equal(EstadoCita.Cancelada, cita.Estado);
            Assert.Equal("Cambio de planes", cita.MotivoCancelacion);
            Assert.True(espacio.Disponible);
            Assert.Null(espacio.IdCita);
            _citaRepoMock.Verify(r => r.GuardarCambiosAsync(cita), Times.Once);
        }

        // ==============================================================
        // 3. REGLAS DE REPROGRAMACIÓN DE CITAS
        // ==============================================================

        [Theory]
        [InlineData(EstadoCita.Cancelada)]
        [InlineData(EstadoCita.Completada)]
        [InlineData(EstadoCita.NoAsistio)]
        public async Task ReprogramarCliente_CitaInvalida_Retorna409Conflict(EstadoCita estadoNoReprogramable)
        {
            using var context = CreateDbContext();
            var service = CrearServicio(context, userId: 10);
            var citaUuid = Guid.NewGuid();

            _citaRepoMock.Setup(r => r.ObtenerPorUuidClienteAsync(citaUuid, 10))
                .ReturnsAsync(new Cita
                {
                    Id = 60,
                    Uuid = citaUuid,
                    IdUsuario = 10,
                    Estado = estadoNoReprogramable
                });

            var resultado = await service.ReprogramarClienteAsync(citaUuid, new ReprogramarCitaDto
            {
                FechaInicio = DateTime.UtcNow.AddDays(2)
            });

            Assert.Equal("409", resultado.Codigo);
            Assert.Equal("Esta cita ya no se puede reprogramar.", resultado.Mensaje);
        }

        // ==============================================================
        // 4. REGLAS DE DIAS CERRADOS EN COMERCIO
        // ==============================================================

        [Fact]
        public async Task HorariosAsync_ComercioCerradoEnFecha_RetornaListaVacia()
        {
            using var context = CreateDbContext();
            var service = CrearServicio(context);
            var prodUuid = Guid.NewGuid();
            var fecha = new DateOnly(2026, 10, 18); // Domingo

            _prodServRepoMock.Setup(r => r.ObtenerReservablePorUuidAsync(prodUuid)).ReturnsAsync(new ProductosServicios
            {
                Id = 1,
                IdComercio = 5,
                DuracionMinutos = 30
            });

            _horarioComercioRepoMock.Setup(r => r.ObtenerAsync(5, fecha.DayOfWeek)).ReturnsAsync(new HorarioComercio
            {
                Abierto = false // Domingo no abre
            });

            var resultado = await service.HorariosAsync(prodUuid, fecha);

            Assert.Equal("200", resultado.Codigo);
            Assert.NotNull(resultado.Respuesta);
            Assert.Empty(resultado.Respuesta);
        }
    }
}
