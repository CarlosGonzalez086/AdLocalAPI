using System.Security.Claims;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AdLocalAPI.Filters;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AdLocalAPI.Tests.Integration;

public class CitasPostgresTests : CheckoutPostgresFixture
{
    private sealed class Reloj : TimeProvider { public DateTimeOffset Utc = new(2030,5,20,18,0,0,TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Utc; }
    private readonly Reloj reloj = new();
    private readonly DateTime inicio = new(2030,5,21,9,0,0);
    private Guid servicioUuid;
    private async Task Seed()
    {
        await using var db = Context(); var p = await db.ProductosServicios.AsTracking().SingleAsync(x => x.Id == 50);
        p.Tipo = TipoProductoServicio.Servicio; p.Modalidad = ModalidadProductoServicio.Reservacion; p.DuracionMinutos = 30; servicioUuid = p.Uuid;
        foreach (DayOfWeek dia in Enum.GetValues<DayOfWeek>()) db.HorarioComercio.Add(new() { ComercioId = 1, Dia = dia, Abierto = true,
            HoraApertura = TimeSpan.FromHours(9), HoraCierre = TimeSpan.FromHours(18) });
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS btree_gist; ALTER TABLE citas ADD CONSTRAINT \"EX_citas_comercio_intervalo\" EXCLUDE USING gist (\"IdComercio\" WITH =, tsrange(\"FechaInicio\", \"FechaFin\", '[)') WITH &&) WHERE (\"Estado\" <> 5);");
    }
    private CitaService Citas(AppDbContext db, long actor = 5, bool dueno = false)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("id", actor.ToString())], "test")) };
        var comercios = new Mock<IComercioRepository>(); comercios.Setup(x => x.PuedeAdministrarAsync(1, actor)).ReturnsAsync(dueno);
        return new(new CitaRepository(db, reloj), comercios.Object, new JwtContext(new HttpContextAccessor { HttpContext = context }), NullLogger<CitaService>.Instance);
    }
    private CrearCitaDto Request(DateTime? fecha = null) => new() { IdempotencyKey = Guid.NewGuid(), ProductoUuid = servicioUuid,
        FechaInicio = fecha ?? inicio, NombrePersona = "Persona sintética", Notas = "Reserva de prueba" };
    private async Task<ApiResponse<CitaDto>> Reservar(CrearCitaDto dto, long actor = 5, params IInterceptor[] interceptors)
    { await using var db = Context(interceptors); return await Citas(db, actor).CrearAsync(dto); }
    private async Task<ApiResponse<CitaDto>> Cambiar(Guid uuid, EstadoCita estado, long version, long actor = 7)
    { await using var db = Context(); return await Citas(db, actor, actor == 7).ActualizarAsync(1, uuid, new() { VersionCita = version, Estado = estado, NombreAtiende = "Operador", Motivo = "Motivo sintético" }); }

    [PostgresCheckoutFact] public async Task DosClientes_UltimoHorario_UnaReservaYSuEspacio()
    {
        await Seed(); var resultados = await Task.WhenAll(Reservar(Request()), Reservar(Request(),6));
        Assert.Single(resultados, x => x.Codigo == "200"); Assert.Single(resultados, x => x.Codigo == "409");
        await using var db = Context(); Assert.Equal(1, await db.Citas.CountAsync()); Assert.Equal(1, await db.HorariosCitaServicio.CountAsync(x => x.IdCita != null)); Assert.Equal(1, await db.CitaHistorial.CountAsync());
    }
    [PostgresCheckoutFact] public async Task MismaClave_DosPeticionesYRespuestaPerdida_RecuperaCita()
    {
        await Seed(); var dto = Request(); var resultados = await Task.WhenAll(Reservar(dto),Reservar(dto));
        Assert.All(resultados,x=>Assert.Equal("200",x.Codigo)); Assert.Equal(resultados[0].Respuesta!.Uuid,resultados[1].Respuesta!.Uuid);
        Assert.Equal(resultados[0].Respuesta!.Uuid,(await Reservar(dto)).Respuesta!.Uuid);
        await using var db = Context(); Assert.Equal(1,await db.Citas.CountAsync());
    }
    [PostgresCheckoutFact] public async Task MismaClave_OtroPayload_Conflicto()
    { await Seed(); var dto = Request(); Assert.Equal("200",(await Reservar(dto)).Codigo); dto.NombrePersona="Otra persona"; Assert.Equal("409",(await Reservar(dto)).Codigo); }
    [PostgresCheckoutFact] public async Task ServiciosDistintos_MismoComercio_NoSeSolapan()
    {
        await Seed(); await using var db = Context(); var p = new ProductosServicios { IdComercio=1, IdUsuario=7, Nombre="Otro servicio", Tipo=TipoProductoServicio.Servicio, Modalidad=ModalidadProductoServicio.Reservacion, DuracionMinutos=60 };
        db.Add(p); await db.SaveChangesAsync(); Assert.Equal("200",(await Reservar(Request(inicio.AddMinutes(30)))).Codigo);
        var dto=Request(); dto.ProductoUuid=p.Uuid; Assert.Equal("409",(await Reservar(dto,6)).Codigo);
    }
    [PostgresCheckoutFact] public async Task IntervalosContiguos_SePermiten()
    { await Seed(); Assert.Equal("200",(await Reservar(Request())).Codigo); Assert.Equal("200",(await Reservar(Request(inicio.AddMinutes(30)),6)).Codigo); }
    [PostgresCheckoutFact] public async Task Disponibilidad_EsLecturaYRespetaDuracionVigente()
    {
        await Seed(); await using var db = Context(); var service=Citas(db);
        Assert.Contains("09:30",(await service.HorariosAsync(servicioUuid,new(2030,5,21))).Respuesta!);
        Assert.Equal(0,await db.HorariosCitaServicio.CountAsync());
        await db.ProductosServicios.Where(p=>p.Id==50).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.DuracionMinutos,45));
        Assert.DoesNotContain("09:30",(await service.HorariosAsync(servicioUuid,new(2030,5,21))).Respuesta!);
        Assert.Equal("409",(await Reservar(Request(inicio.AddMinutes(30)))).Codigo);
    }
    [PostgresCheckoutFact] public async Task FechaPasadaUtcSegundosYCierre_NoReservan()
    {
        await Seed(); foreach(var fecha in new[]{inicio.AddDays(-2),DateTime.SpecifyKind(inicio,DateTimeKind.Utc),inicio.AddSeconds(1),inicio.Date.AddHours(18)})
            Assert.NotEqual("200",(await Reservar(Request(fecha))).Codigo);
        await using var db=Context(); Assert.Equal(0,await db.Citas.CountAsync());
    }
    [PostgresCheckoutFact] public async Task Cancelacion_Atomica_LiberaEspacioYPermiteNuevaReserva()
    {
        await Seed(); var cita=(await Reservar(Request())).Respuesta!; await using(var db=Context())
            Assert.Equal("200",(await Citas(db).CancelarClienteAsync(cita.Uuid,new(){VersionCita=0,Motivo="No puedo asistir"})).Codigo);
        Assert.Equal("200",(await Reservar(Request(),6)).Codigo); await using var check=Context(); Assert.Equal(2,await check.Citas.CountAsync());
        Assert.Equal(1,await check.HorariosCitaServicio.CountAsync(x=>x.IdCita!=null));
    }
    [PostgresCheckoutFact] public async Task ReprogramacionFallida_ConservaCitaYEspacioAnterior()
    {
        await Seed(); var cita=(await Reservar(Request())).Respuesta!; await Reservar(Request(inicio.AddHours(1)),6);
        await using var db=Context(); var r=await Citas(db).ReprogramarClienteAsync(cita.Uuid,new(){VersionCita=0,FechaInicio=inicio.AddHours(1)});
        Assert.Equal("409",r.Codigo); Assert.Equal(inicio,(await db.Citas.SingleAsync(x=>x.Uuid==cita.Uuid)).FechaInicio);
        Assert.Equal(2,await db.HorariosCitaServicio.CountAsync(x=>x.IdCita!=null));
    }
    [PostgresCheckoutFact] public async Task Reprogramar_MueveEspacio_Audita_YExcluyeSuPropiaCita()
    {
        await Seed(); var cita=(await Reservar(Request())).Respuesta!; await using var db=Context();
        Assert.Contains("09:00",(await Citas(db).HorariosAsync(servicioUuid,new(2030,5,21),cita.Uuid)).Respuesta!);
        Assert.Equal("200",(await Citas(db).ReprogramarClienteAsync(cita.Uuid,new(){VersionCita=0,FechaInicio=inicio.AddHours(1)})).Codigo);
        Assert.Equal(1,await db.HorariosCitaServicio.CountAsync(x=>x.IdCita!=null)); Assert.Equal(2,await db.CitaHistorial.CountAsync());
    }
    [PostgresCheckoutFact] public async Task DosOperadores_VersionAnterior_NoPisaEstado()
    {
        await Seed(); var cita=(await Reservar(Request())).Respuesta!;
        var results=await Task.WhenAll(Cambiar(cita.Uuid,EstadoCita.Confirmada,0),Cambiar(cita.Uuid,EstadoCita.Cancelada,0));
        Assert.Single(results,x=>x.Codigo=="200"); Assert.Single(results,x=>x.Codigo=="409");
    }
    [PostgresCheckoutFact] public async Task Terminal_NoSeReabreYCicloNoSaltaEstados()
    {
        await Seed(); var cita=(await Reservar(Request())).Respuesta!;
        Assert.Equal("409",(await Cambiar(cita.Uuid,EstadoCita.Completada,0)).Codigo);
        Assert.Equal("200",(await Cambiar(cita.Uuid,EstadoCita.Confirmada,0)).Codigo);
        Assert.Equal("409",(await Cambiar(cita.Uuid,EstadoCita.EnAtencion,1)).Codigo);
        reloj.Utc=new(2030,5,21,15,0,0,TimeSpan.Zero); Assert.Equal("200",(await Cambiar(cita.Uuid,EstadoCita.EnAtencion,1)).Codigo);
        Assert.Equal("409",(await Cambiar(cita.Uuid,EstadoCita.Completada,2)).Codigo);
        reloj.Utc=reloj.Utc.AddHours(1); Assert.Equal("200",(await Cambiar(cita.Uuid,EstadoCita.Completada,2)).Codigo);
        Assert.Equal("409",(await Cambiar(cita.Uuid,EstadoCita.Pendiente,3)).Codigo);
    }
    [PostgresCheckoutFact] public async Task ClienteAjeno_NoLeeDisponibilidadDeReprogramacionNiMuta()
    {
        await Seed(); var cita=(await Reservar(Request())).Respuesta!; await using var db=Context();
        Assert.Equal("404",(await Citas(db,6).HorariosAsync(servicioUuid,new(2030,5,21),cita.Uuid)).Codigo);
        Assert.Equal("404",(await Citas(db,6).CancelarClienteAsync(cita.Uuid,new(){VersionCita=0,Motivo="Ajeno"})).Codigo);
        Assert.Equal("403",(await Cambiar(cita.Uuid,EstadoCita.Cancelada,0,6)).Codigo);
    }
    private sealed class FallaSegundoSave : SaveChangesInterceptor
    {
        private int saves;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,InterceptionResult<int> result,CancellationToken cancellationToken=default)
        { if(++saves==2) throw new IOException("Fallo sintético tras insertar cita"); return new(result); }
    }
    [PostgresCheckoutFact] public async Task FallaAntesDeGuardarEspacio_RevierteCitaClaveYHistorial()
    {
        await Seed(); var dto=Request(); Assert.Equal("500",(await Reservar(dto,5,new FallaSegundoSave())).Codigo);
        await using(var db=Context()) { Assert.Equal(0,await db.Citas.CountAsync()); Assert.Equal(0,await db.HorariosCitaServicio.CountAsync()); }
        Assert.Equal("200",(await Reservar(dto)).Codigo);
    }
    [PostgresCheckoutFact] public async Task BaseDeDatos_RechazaSolapamientoAunqueSeOmitaRepositorio()
    {
        await Seed(); await Reservar(Request()); await using var db=Context();
        db.Citas.Add(new(){IdUsuario=6,IdComercio=1,IdProductoServicio=50,NombrePersona="Inserción externa",FechaInicio=inicio,FechaFin=inicio.AddHours(1)});
        var e=await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync()); Assert.Equal("23P01",Assert.IsType<PostgresException>(e.InnerException).SqlState);
    }
    [PostgresCheckoutFact] public async Task CambioHorario_NoDejaReservaVigenteFueraDeAtencion()
    {
        await Seed(); await Reservar(Request()); await using var db=Context();
        await Assert.ThrowsAsync<CitaOperacionException>(()=>new HorarioComercioRepository(db).ActualizarHorariosAsync(1,[]));
        Assert.Equal(7,await db.HorarioComercio.CountAsync());
    }
    [PostgresCheckoutFact] public async Task ServicioDeshabilitado_NoAceptaNuevaCitaPeroConservaHistoria()
    {
        await Seed(); var dto=Request(); await Reservar(dto); await using var db=Context();
        await db.ProductosServicios.Where(p=>p.Id==50).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.Eliminado,true));
        Assert.Equal("200",(await Reservar(dto)).Codigo); Assert.Single((await Citas(db).MisCitasAsync()).Respuesta!);
        Assert.Equal("404",(await Reservar(Request(inicio.AddHours(1)))).Codigo);
    }
    [PostgresCheckoutFact] public async Task ReprogramarContraNuevaReserva_UnSoloGanador_ConservaAnteriorSiPierde()
    {
        await Seed(); var cita=(await Reservar(Request())).Respuesta!;
        async Task<ApiResponse<CitaDto>> Mover() { await using var db=Context(); return await Citas(db).ReprogramarClienteAsync(cita.Uuid,new(){VersionCita=0,FechaInicio=inicio.AddHours(1)}); }
        var results=await Task.WhenAll(Mover(),Reservar(Request(inicio.AddHours(1)),6));
        Assert.Single(results,x=>x.Codigo=="200"); Assert.Single(results,x=>x.Codigo=="409");
        await using var check=Context(); Assert.Equal(1,await check.Citas.CountAsync(x=>x.FechaInicio==inicio.AddHours(1)));
        if(results[0].Codigo=="409") Assert.Equal(inicio,(await check.Citas.SingleAsync(x=>x.Uuid==cita.Uuid)).FechaInicio);
    }
    private sealed class FallaSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,InterceptionResult<int> result,CancellationToken cancellationToken=default)
            => throw new IOException("Fallo sintético después de liberar espacio");
    }
    [PostgresCheckoutFact] public async Task FallaReprogramacion_RevierteLiberacionDeEspacioAnterior()
    {
        await Seed(); var cita=(await Reservar(Request())).Respuesta!; await using(var db=Context(new FallaSave()))
            Assert.Equal("500",(await Citas(db).ReprogramarClienteAsync(cita.Uuid,new(){VersionCita=0,FechaInicio=inicio.AddHours(1)})).Codigo);
        await using var check=Context(); var espacio=await check.HorariosCitaServicio.SingleAsync(); Assert.False(espacio.Disponible); Assert.NotNull(espacio.IdCita);
        Assert.Equal(inicio,(await check.Citas.SingleAsync()).FechaInicio); Assert.Equal(1,await check.CitaHistorial.CountAsync());
    }
    [PostgresCheckoutFact] public async Task EspacioLegacyObsoleto_NoEsFuenteDeDisponibilidad()
    {
        await Seed(); await using var db=Context(); db.HorariosCitaServicio.Add(new(){IdProductoServicio=50,IdComercio=1,Fecha=new(2030,5,21),HoraInicio=inicio.TimeOfDay,HoraFin=inicio.AddHours(3).TimeOfDay,Disponible=false}); await db.SaveChangesAsync();
        Assert.Equal("200",(await Reservar(Request())).Codigo); var espacio=await db.HorariosCitaServicio.SingleAsync(); Assert.Equal(inicio.AddMinutes(30).TimeOfDay,espacio.HoraFin);
        await db.HorarioComercio.Where(h=>h.Dia==inicio.DayOfWeek).ExecuteUpdateAsync(s=>s.SetProperty(h=>h.Abierto,false));
        Assert.Empty((await Citas(db).HorariosAsync(servicioUuid,new(2030,5,21))).Respuesta!);
    }
    private async Task<string> PrepararMigracion(bool duplicada)
    {
        await Seed(); await Reservar(Request()); await using var db=Context();
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE citas DROP CONSTRAINT \"EX_citas_comercio_intervalo\", DROP CONSTRAINT \"CK_citas_estado\", DROP CONSTRAINT \"CK_citas_intervalo\"; DROP TABLE cita_historial; ALTER TABLE citas DROP COLUMN \"Version\", DROP COLUMN \"RequestHash\", DROP COLUMN \"IdempotencyKey\";");
        if(duplicada) await db.Database.ExecuteSqlRawAsync("INSERT INTO citas (\"Uuid\",\"IdUsuario\",\"IdComercio\",\"IdProductoServicio\",\"NombrePersona\",\"FechaInicio\",\"FechaFin\",\"Estado\",\"FechaCreacion\") SELECT gen_random_uuid(),6,\"IdComercio\",\"IdProductoServicio\",'Legacy duplicada',\"FechaInicio\",\"FechaFin\",1,now() FROM citas LIMIT 1;");
        await db.Database.ExecuteSqlRawAsync(db.GetService<IHistoryRepository>().GetCreateScript());
        return db.GetService<IMigrator>().GenerateScript("20261012215314_PedidosInventarioReembolsos","20261013155802_IntegridadCitas",MigrationsSqlGenerationOptions.Idempotent);
    }
    [PostgresCheckoutFact] public async Task Migracion_SeReejecuta_ConservaCitaLegacyYSusHoras()
    {
        var sql=await PrepararMigracion(false); Assert.DoesNotContain("Publicidades",sql); Assert.DoesNotContain("Eventos",sql);
        await using var db=Context(); await db.Database.ExecuteSqlRawAsync(sql); await db.Database.ExecuteSqlRawAsync(sql);
        var cita=await db.Citas.SingleAsync(); Assert.Equal(inicio,cita.FechaInicio); Assert.Null(cita.IdempotencyKey); Assert.Equal(0,cita.Version);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root != null && !File.Exists(Path.Combine(root.FullName,"scripts","citas-integridad-preflight.sql"))) root=root.Parent;
        Assert.NotNull(root);
        await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(Path.Combine(root!.FullName,"scripts","citas-integridad-preflight.sql")));
    }
    [PostgresCheckoutFact] public async Task Migracion_ConflictoHistorico_DetieneSinBorrarCitas()
    {
        var sql=await PrepararMigracion(true); await using(var db=Context()) await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlRawAsync(sql));
        await using var check=Context(); Assert.Equal(2,await check.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM citas").SingleAsync());
        Assert.Equal(0,await check.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM information_schema.columns WHERE table_name='citas' AND column_name='Version'").SingleAsync());
    }
    [PostgresCheckoutFact] public async Task HttpReal_ClaveVersionObligatoria_RolesYCancelacion()
    {
        await Seed(); var builder=WebApplication.CreateBuilder(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers(o=>o.Filters.Add<ValidationFilter>()).AddApplicationPart(typeof(AdLocalAPI.Controllers.CitasController).Assembly);
        builder.Services.AddValidatorsFromAssemblyContaining<CrearCitaDtoValidator>(); builder.Services.AddHttpContextAccessor(); builder.Services.AddScoped<JwtContext>(); builder.Services.AddScoped(_=>Context());
        builder.Services.AddScoped<ICitaRepository>(sp=>new CitaRepository(sp.GetRequiredService<AppDbContext>(),reloj)); builder.Services.AddScoped<ICitaService,CitaService>();
        builder.Services.AddSingleton(new Mock<IComercioRepository>().Object);
        builder.Services.AddScoped<AdLocalAPI.Interfaces.Services.IComercioService>(sp => new ComercioService(
            Comercios(sp.GetRequiredService<AppDbContext>()),sp.GetRequiredService<JwtContext>(),null!,null!,null!,null!,null!,null!,null!,null!,null!,null!,null!,null!,null!));
        const string key="Phase4SyntheticJwtForContractOnly123456789012345!";
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>o.TokenValidationParameters=new(){ValidateIssuer=false,ValidateAudience=false,ValidateIssuerSigningKey=true,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))});
        builder.Services.AddAuthorization(); await using var app=builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
        using var client=new HttpClient { BaseAddress=new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
        var dto=Request(); Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsJsonAsync("/api/Citas",dto)).StatusCode);
        void Login(string rol,string id="5") { var token=new JwtSecurityToken(claims:[new Claim("id",id),new Claim(ClaimTypes.Role,rol)],expires:DateTime.UtcNow.AddMinutes(5),signingCredentials:new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),SecurityAlgorithms.HmacSha256)); client.DefaultRequestHeaders.Authorization=new("Bearer",new JwtSecurityTokenHandler().WriteToken(token)); }
        Login("Comercio"); Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/Citas",dto)).StatusCode);
        Login("Cliente"); Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/Citas",new{dto.ProductoUuid,dto.FechaInicio,dto.NombrePersona,dto.ZonaHoraria})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/Citas",new{dto.IdempotencyKey,dto.ProductoUuid,dto.FechaInicio,dto.NombrePersona})).StatusCode);
        var r=await client.PostAsJsonAsync("/api/Citas",dto); Assert.Equal(HttpStatusCode.OK,r.StatusCode); var cita=(await r.Content.ReadFromJsonAsync<ApiResponse<CitaDto>>())!.Respuesta!;
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync($"/api/Citas/{cita.Uuid}/cancelar",new{motivo="No puedo asistir"})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync($"/api/Citas/{cita.Uuid}/cancelar",new{versionCita=0,motivo="No puedo asistir"})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/Citas",dto)).StatusCode);
        var zona=new{zonaHoraria="America/Cancun",zonaHorariaAnterior="America/Mexico_City"};
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync("/api/Comercios/1/zona-horaria",zona)).StatusCode);
        Login("Colaborador","7"); Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync("/api/Comercios/1/zona-horaria",zona)).StatusCode);
        Login("Comercio","6"); Assert.Equal(HttpStatusCode.NotFound,(await client.PutAsJsonAsync("/api/Comercios/1/zona-horaria",zona)).StatusCode);
        Login("Comercio","7"); Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync("/api/Comercios/1/zona-horaria",zona)).StatusCode);
        await app.StopAsync();
    }
    private ComercioRepository Comercios(AppDbContext db) => new(db,null!,new Mock<Amazon.S3.IAmazonS3>().Object);
    private async Task<string> Zona(string nueva,string anterior="America/Mexico_City",long actor=7)
    { await using var db=Context(); return await Comercios(db).ActualizarZonaHorariaAsync(1,actor,new(){ZonaHoraria=nueva,ZonaHorariaAnterior=anterior}); }
    [PostgresCheckoutFact] public async Task ZonaPorComercio_ValidaContraSuHoraLocal_NoContraServidor()
    {
        await Seed(); await Zona("America/Tijuana"); reloj.Utc=new(2030,5,21,15,0,0,TimeSpan.Zero); // 08:00 Tijuana; 09:00 CDMX.
        var dto=Request(); dto.ZonaHoraria="America/Tijuana"; var r=await Reservar(dto); Assert.Equal("200",r.Codigo); Assert.Equal("America/Tijuana",r.Respuesta!.ZonaHoraria);
        Assert.Equal(inicio,r.Respuesta.FechaInicio); Assert.Equal("409",(await Reservar(Request(inicio.AddHours(1)),6)).Codigo);
    }
    [PostgresCheckoutFact] public async Task CambioZona_ConCitasActivas_SeBloquea_SinDesplazarlas()
    {
        await Seed(); var cita=(await Reservar(Request())).Respuesta!;
        var e=await Assert.ThrowsAsync<CitaOperacionException>(()=>Zona("America/Cancun")); Assert.Equal("409",e.Codigo);
        await using var db=Context(); Assert.Equal(inicio,(await db.Citas.SingleAsync()).FechaInicio); Assert.Equal("America/Mexico_City",(await db.Comercios.SingleAsync()).ZonaHoraria);
    }
    [PostgresCheckoutFact] public async Task CambioZona_SinActivas_PreservaZonaHistoricaYRecuperacion()
    {
        await Seed(); var dto=Request(); var cita=(await Reservar(dto)).Respuesta!; await Cambiar(cita.Uuid,EstadoCita.Cancelada,0);
        await Zona("America/Cancun"); var recuperada=(await Reservar(dto)).Respuesta!;
        Assert.Equal(EstadoCita.Cancelada,recuperada.Estado); Assert.Equal("America/Mexico_City",recuperada.ZonaHoraria);
        var nueva=Request(inicio.AddHours(1)); nueva.ZonaHoraria="America/Cancun"; Assert.Equal("America/Cancun",(await Reservar(nueva)).Respuesta!.ZonaHoraria);
    }
    [PostgresCheckoutFact] public async Task CambioZonaContraReserva_UnaSolaPoliticaConfirmada()
    {
        await Seed(); async Task<string> CambiarZona() { try { await Zona("America/Tijuana"); return "200"; } catch(CitaOperacionException e) { return e.Codigo; } }
        async Task<string> Crear() => (await Reservar(Request())).Codigo;
        var resultados=await Task.WhenAll(CambiarZona(),Crear()); Assert.Single(resultados,x=>x=="200"); Assert.Single(resultados,x=>x=="409");
        await using var db=Context(); if(await db.Citas.AnyAsync()) Assert.Equal((await db.Comercios.SingleAsync()).ZonaHoraria,(await db.Citas.SingleAsync()).ZonaHoraria);
    }
    [PostgresCheckoutFact] public async Task CambioZona_AjenoInvalidoODesdeVistaVieja_NoModificaConfiguracion()
    {
        await Seed(); Assert.Equal("404",(await Assert.ThrowsAsync<CitaOperacionException>(()=>Zona("America/Cancun",actor:6))).Codigo);
        Assert.Equal("400",(await Assert.ThrowsAsync<CitaOperacionException>(()=>Zona("Zona/Inventada"))).Codigo);
        await Zona("America/Cancun"); Assert.Equal("409",(await Assert.ThrowsAsync<CitaOperacionException>(()=>Zona("America/Tijuana"))).Codigo);
    }
    [PostgresCheckoutFact] public async Task EdicionGeneralObsoleta_NoSobrescribeZonaConfirmada()
    {
        await Seed(); await using var db=Context(); var vieja=await db.Comercios.SingleAsync(); await Zona("America/Cancun");
        vieja.Nombre="Nombre editado"; await Comercios(db).UpdateAsync(vieja); await using var check=Context();
        Assert.Equal("America/Cancun",(await check.Comercios.SingleAsync()).ZonaHoraria);
    }
    [PostgresCheckoutFact] public async Task HorarioEstacional_NoOfreceNiReservaHoraInexistenteORepetida()
    {
        await Seed(); await Zona("America/Tijuana"); reloj.Utc=new(2030,3,1,12,0,0,TimeSpan.Zero);
        await using var db=Context(); await db.HorarioComercio.ExecuteUpdateAsync(s=>s.SetProperty(h=>h.HoraApertura,TimeSpan.Zero).SetProperty(h=>h.HoraCierre,TimeSpan.FromHours(6)));
        var primavera=new DateTime(2030,3,10,2,0,0); var dto=Request(primavera); dto.ZonaHoraria="America/Tijuana";
        Assert.Equal("400",(await Reservar(dto)).Codigo); var horas=(await Citas(db).HorariosAsync(servicioUuid,new(2030,3,10))).Respuesta!; Assert.DoesNotContain("02:00",horas);
        reloj.Utc=new(2030,10,20,12,0,0,TimeSpan.Zero); dto=Request(new(2030,11,3,1,0,0)); dto.ZonaHoraria="America/Tijuana";
        Assert.Equal("400",(await Reservar(dto)).Codigo); Assert.DoesNotContain("01:00",(await Citas(db).HorariosAsync(servicioUuid,new(2030,11,3))).Respuesta!);
    }
    [PostgresCheckoutFact] public async Task Reprogramacion_ZonaEquivocada_NoReinterpretaLaCita()
    {
        await Seed(); var cita=(await Reservar(Request())).Respuesta!; await using var db=Context();
        Assert.Equal("409",(await Citas(db).ReprogramarClienteAsync(cita.Uuid,new(){VersionCita=0,FechaInicio=inicio.AddHours(1),ZonaHoraria="America/Tijuana"})).Codigo);
        Assert.Equal(inicio,(await db.Citas.SingleAsync()).FechaInicio);
    }
    [PostgresCheckoutFact] public async Task MigracionZonas_SeReejecuta_SinMoverHorasNiModificarDatosAjenos()
    {
        await Seed(); await Reservar(Request()); await using var db=Context();
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE citas DROP COLUMN \"ZonaHoraria\"; ALTER TABLE \"Comercios\" DROP COLUMN \"ZonaHoraria\";");
        await db.Database.ExecuteSqlRawAsync(db.GetService<IHistoryRepository>().GetCreateScript());
        var sql=db.GetService<IMigrator>().GenerateScript("20261013155802_IntegridadCitas","20261013164345_ZonasHorariasCitas",MigrationsSqlGenerationOptions.Idempotent);
        Assert.DoesNotContain("Publicidades",sql); Assert.DoesNotContain("Eventos",sql); await db.Database.ExecuteSqlRawAsync(sql); await db.Database.ExecuteSqlRawAsync(sql);
        Assert.Equal(inicio,(await db.Citas.SingleAsync()).FechaInicio); Assert.Equal("America/Mexico_City",(await db.Citas.SingleAsync()).ZonaHoraria);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root != null && !File.Exists(Path.Combine(root.FullName,"scripts","citas-zonas-horarias-preflight.sql"))) root=root.Parent;
        Assert.NotNull(root);
        await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(Path.Combine(root!.FullName,"scripts","citas-zonas-horarias-preflight.sql")));
    }
}
