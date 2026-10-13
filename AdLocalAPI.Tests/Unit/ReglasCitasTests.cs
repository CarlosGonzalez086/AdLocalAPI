using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Unit;

public class ReglasCitasTests
{
    [Theory]
    [InlineData(EstadoCita.Completada)] [InlineData(EstadoCita.Cancelada)] [InlineData(EstadoCita.NoAsistio)]
    public void Terminal_NoPuedeReabrirse(EstadoCita estado) => Assert.Empty(CitaOperacion.Acciones(estado));

    [Fact] public void Estados_NoPermitenSaltarAtencion()
    {
        Assert.DoesNotContain(EstadoCita.Completada, CitaOperacion.Acciones(EstadoCita.Pendiente));
        Assert.DoesNotContain(EstadoCita.EnAtencion, CitaOperacion.Acciones(EstadoCita.Pendiente));
        Assert.Equal([EstadoCita.Completada], CitaOperacion.Acciones(EstadoCita.EnAtencion));
    }
    [Theory] [InlineData(0)] [InlineData(-30)] [InlineData(1500)]
    public void DuracionInvalida_NoGeneraEspacios(int minutos) => Assert.Empty(CitaOperacion.Espacios(
        new() { DuracionMinutos = minutos }, new() { Abierto = true, HoraApertura = TimeSpan.FromHours(9), HoraCierre = TimeSpan.FromHours(12) }, new(2030, 5, 21)));
    [Fact] public void Duracion_NoExcedeCierre()
    {
        var espacios = CitaOperacion.Espacios(new() { DuracionMinutos = 45 },
            new() { Abierto = true, HoraApertura = TimeSpan.FromHours(9), HoraCierre = TimeSpan.FromHours(11) }, new(2030, 5, 21));
        Assert.Equal(2, espacios.Count); Assert.Equal(new DateTime(2030, 5, 21, 9, 45, 0), espacios[1]);
    }
    [Fact] public void FechaUtc_NoSeReinterpretaComoLocal() => Assert.Throws<CitaOperacionException>(() =>
        CitaOperacion.Fecha(new DateTime(2030, 5, 21, 9, 0, 0, DateTimeKind.Utc), new(2030, 5, 20)));
    [Fact] public void FechaConSegundos_NoSeRedondeaAlReservar() => Assert.Throws<CitaOperacionException>(() =>
        CitaOperacion.Fecha(new DateTime(2030, 5, 21, 9, 0, 1), new(2030, 5, 20)));
    [Fact] public void Huella_VinculaPersonaHorarioYServicio()
    {
        var dto = new CrearCitaDto { ProductoUuid = Guid.NewGuid(), NombrePersona = " Ana ", FechaInicio = new(2030, 5, 21, 9, 0, 0) };
        var hash = CitaOperacion.Hash(dto); dto.NombrePersona = "Ana"; Assert.Equal(hash, CitaOperacion.Hash(dto));
        dto.FechaInicio = dto.FechaInicio.AddMinutes(30); Assert.NotEqual(hash, CitaOperacion.Hash(dto));
    }
    [Fact] public async Task ComercioAjeno_NoInvocaEscritura()
    {
        var citas = new Mock<ICitaRepository>(MockBehavior.Strict);
        var comercios = new Mock<IComercioRepository>();
        var http = new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim("id", "6")], "test")) };
        var service = new CitaService(citas.Object, comercios.Object, new JwtContext(new HttpContextAccessor { HttpContext = http }), NullLogger<CitaService>.Instance);
        Assert.Equal("403", (await service.ActualizarAsync(1, Guid.NewGuid(), new())).Codigo);
    }
    [Fact] public void HorasMundiales_CompartenInstanteSinDependerDelServidor()
    {
        var reloj = new RelojUtc(); Assert.Equal(23,CitaOperacion.Ahora(reloj,"America/Tijuana").Hour); Assert.Equal(1,CitaOperacion.Ahora(reloj,"America/Cancun").Hour);
    }
    private sealed class RelojUtc : TimeProvider { public override DateTimeOffset GetUtcNow()=>new(2026,10,13,6,30,0,TimeSpan.Zero); }
    [Theory] [InlineData("Zona/Inventada")] [InlineData("Pacific Standard Time")] [InlineData("")]
    public void ZonaInvalida_NoSeConvierteAUnaZonaPorDefecto(string zona) => Assert.False(CitaOperacion.ZonaValida(zona));
    [Fact] public void HorarioEstacional_NoAceptaDuracionCivilDistintaDeReal() => Assert.False(CitaOperacion.IntervaloValido(
        new(2030,3,10,1,0,0),new(2030,3,10,3,0,0),"America/Tijuana"));
}
