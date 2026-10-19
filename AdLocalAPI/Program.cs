using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Interfaces.Location;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Interfaces.Repository;
using AdLocalAPI.Interfaces.Tarjetas;
using AdLocalAPI.Interfaces.TipoComercio;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Middlewares;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Filters;
using AdLocalAPI.Utils;
using AdLocalAPI.Validators;
using Amazon.Runtime;
using Amazon.S3;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configuración local para ejecución de la API.
// La configuración para migraciones se carga también desde AppDbContextFactory.
builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: true,
    reloadOnChange: builder.Environment.IsDevelopment()
);

// Conservar el cargador personalizado del proyecto.
ConfiguracionSegura.CargarLocal(
    builder.Configuration,
    builder.Environment,
    args
);

// Las variables de entorno tienen la última prioridad en la API.
builder.Configuration.AddEnvironmentVariables();

//Singlenton variable
builder.Services.AddSingleton<StripeSettings>();

// ======================================================
// VARIABLES DE ENTORNO (Docker / Producción / Local)
// ======================================================

// Puerto (Railway / Docker)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// JWT
var jwtKey = ConfiguracionSegura.Requerir(Environment.GetEnvironmentVariable("JWT__Key")
    ?? builder.Configuration["Jwt:Key"], "Jwt__Key");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt__Key debe tener al menos 32 bytes.");

var jwtIssuer = Environment.GetEnvironmentVariable("JWT__Issuer")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "AdLocalAPI";

var jwtAudience = Environment.GetEnvironmentVariable("JWT__Audience")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "AdLocal";

//var webhookSecret = ConfiguracionSegura.Requerir(Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
//    ?? builder.Configuration["Stripe:WebhookSecret"], "STRIPE_WEBHOOK_SECRET / Stripe__WebhookSecret");

// PostgreSQL / Supabase
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontró la conexión PostgreSQL (DB_CONNECTION o ConnectionStrings:DefaultConnection)."
    );
}

// Emisión y validación deben emplear exactamente la misma configuración efectiva.
builder.Configuration["Jwt:Key"] = jwtKey;
builder.Configuration["Jwt:Issuer"] = jwtIssuer;
builder.Configuration["Jwt:Audience"] = jwtAudience;
//builder.Configuration["Stripe:WebhookSecret"] = webhookSecret;

Stripe.StripeConfiguration.ApiKey =
   builder.Configuration["Stripe:SecretKey"];


// ======================================================
// AUTH JWT
// ======================================================

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        ),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    options.Events = TokenSecurityValidator.CrearEventos();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = builder.Configuration;

    var accessKey = config["R2:AccessKeyId"];
    var secretKey = config["R2:SecretAccessKey"];
    var accountId = config["R2:AccountId"];

    var credentials = new BasicAWSCredentials(accessKey, secretKey);

    var s3Config = new AmazonS3Config
    {
        ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
        AuthenticationRegion = "auto",
        ForcePathStyle = true,

        UseHttp = false,


        MaxErrorRetry = 5
    };



    return new AmazonS3Client(credentials, s3Config);
});

// ======================================================
// ENTITY FRAMEWORK CORE - PostgreSQL (Supabase)
// ======================================================
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsql =>
        {
            npgsql.UseNetTopologySuite();
            npgsql.CommandTimeout(30);
            npgsql.ExecutionStrategy(deps =>
                new NonRetryingExecutionStrategy(deps)
            );
        });

    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

builder.Services.AddValidatorsFromAssemblyContaining<ProductosServiciosDtoValidator>();

// ======================================================
// SERVICIOS Y REPOSITORIOS
// ======================================================

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<JwtContext>();

builder.Services.AddScoped<IComercioRepository, ComercioRepository>();
builder.Services.AddScoped<ComercioRepository>();
builder.Services.AddScoped<IComercioService, ComercioService>();
builder.Services.AddScoped<ComercioService>();
builder.Services.AddScoped<CitaService>();
builder.Services.AddScoped<IRelComercioImagenRepositorio, RelComercioImagenRepositorio>();


builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

builder.Services.AddScoped<IProductosServiciosRepository, ProductosServiciosRepository>();
builder.Services.AddScoped<IProductosServiciosService, ProductosServiciosService>();
builder.Services.AddScoped<IHorarioComercioRepository, HorarioComercioRepository>();


builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<PlanRepository>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<AdLocalAPI.Services.PlanService>();

builder.Services.AddScoped<ISuscripcionRepository, SuscripcionRepository>();
builder.Services.AddScoped<SuscripcionRepository>();
builder.Services.AddScoped<SuscripcionService>();

builder.Services.AddScoped<StripeService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IGeoLocationService, GeoLocationService>();
builder.Services.AddScoped<GeoLocationService>(sp => (GeoLocationService)sp.GetRequiredService<IGeoLocationService>());

builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();

builder.Services.AddScoped<ITarjetaService, TarjetaService>();
builder.Services.AddScoped<ITarjetaRepository, TarjetaRepository>();
builder.Services.AddScoped<IStripeService, StripeService>();

builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<ILocationService, LocationService>();

builder.Services.AddScoped<ITipoComercioRepository, TipoComercioRepository>();
builder.Services.AddScoped<ITipoComercioService, TipoComercioService>();


builder.Services.AddScoped<ICalificacionComentarioRepository, CalificacionComentarioRepository>();
builder.Services.AddScoped<CalificacionComentarioRepository>();
builder.Services.AddScoped<ICalificacionComentarioService, CalificacionComentarioService>();
builder.Services.AddScoped<CalificacionComentarioService>();

builder.Services.AddSingleton<StripeConfigProvider>();
builder.Services.AddSingleton<ClavesConfigProvider>();


builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddScoped<IComercioVisitaService, ComercioVisitaService>();
builder.Services.AddScoped<ComercioVisitaService>();
builder.Services.AddScoped<IComercioVisitaRepository, ComercioVisitaRepository>();
builder.Services.AddScoped<ComercioVisitaRepository>();

builder.Services.AddScoped<IUsoCodigoReferidoRepository, UsoCodigoReferidoRepository>();
builder.Services.AddScoped<UsoCodigoReferidoRepository>();
builder.Services.AddScoped<IUsoCodigoReferidoService, UsoCodigoReferidoService>();
builder.Services.AddScoped<UsoCodigoReferidoService>();

builder.Services.AddScoped<IBeneficiosService, BeneficiosServices>();
builder.Services.AddScoped<BeneficiosServices>();

builder.Services.AddScoped<ISuscripcionService, SuscripcionService>();
builder.Services.AddScoped<IStripeReconciliationService, StripeReconciliationService>();

builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();

builder.Services.AddScoped<ICarritoService, CarritoService>();
builder.Services.AddScoped<ICarritoRepository, CarritoRepository>();

builder.Services.AddScoped<IDireccionUsuarioRepository, DireccionUsuarioRepository>();
builder.Services.AddScoped<IDireccionUsuarioService, DireccionUsuarioService>();

builder.Services.AddScoped<IConfiguracionPagoComercioRepository, ConfiguracionPagoComercioRepository>();
builder.Services.AddScoped<IConfiguracionPagoComercioService, ConfiguracionPagoComercioService>();

builder.Services.AddScoped<ICuentaBancariaComercioRepository, CuentaBancariaComercioRepository>();
builder.Services.AddScoped<ICuentaBancariaComercioService, CuentaBancariaComercioService>();

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<ICheckoutService, AdLocalAPI.Services.CheckoutService>();
builder.Services.AddScoped<IComprobantePagoService, ComprobantePagoService>();
builder.Services.AddScoped<IPedidoClienteService, PedidoClienteService>();

builder.Services.AddScoped<IPedidoComercioRepository, PedidoComercioRepository>();
builder.Services.AddScoped<IPedidoComercioService, PedidoComercioService>();
builder.Services.AddScoped<INotificacionRepository, NotificacionRepository>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddScoped<IComisionRepository, ComisionRepository>();
builder.Services.AddScoped<IComisionService, ComisionService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<AdLocalAPI.Interfaces.Services.IRefreshTokenService, RefreshTokenService>();

builder.Services.AddScoped<ICitaService, CitaService>();
builder.Services.AddScoped<ICitaRepository, CitaRepository>();
builder.Services.AddScoped<IHorarioCitaServicioRepository, HorarioCitaServicioRepository>();

builder.Services.AddScoped<ICotizacionRepository, CotizacionRepository>();
builder.Services.AddScoped<ICotizacionService, CotizacionService>();

builder.Services.AddScoped<ICuentaBancariaAdLocalRepository, CuentaBancariaAdLocalRepository>();
builder.Services.AddScoped<ICuentaBancariaAdLocalService, CuentaBancariaAdLocalService>();

builder.Services.AddScoped<IPagoComisionRepository, PagoComisionRepository>();
builder.Services.AddScoped<IPagoComisionService, PagoComisionService>();

builder.Services.AddScoped<IStripeWebhookEventRepository, StripeWebhookEventRepository>();
builder.Services.AddScoped<IWebhookService, WebhookService>();




builder.Services.AddSingleton<AppConfigState>();

// ======================================================
// CONTROLLERS + SWAGGER
// ======================================================

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var primerError = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => !string.IsNullOrEmpty(e.ErrorMessage) ? e.ErrorMessage : e.Exception?.Message)
            .FirstOrDefault(msg => !string.IsNullOrWhiteSpace(msg)) ?? "Solicitud inválida o malformada.";
        return new BadRequestObjectResult(AdLocalAPI.Models.ApiResponse.Error("400", primerError));
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AdLocal API",
        Version = "v1"
    });
});



// ======================================================
// CORS
// ======================================================

var defaultOrigins = string.Join(
    ",",
    "http://localhost:4321",
    "http://127.0.0.1:4321",
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "https://adlocal.store",
    "https://panel.adlocal.store",
    "https://www.adlocal.store",
    "https://ad-local-gamma.vercel.app",
    "https://adlocalweb.jcarlosgonzalez086.workers.dev",
    "https://adlocal.jcarlosgonzalez086.workers.dev"
);

var corsOrigins =
    Environment.GetEnvironmentVariable(
        "CORS__ALLOWED_ORIGINS"
    )
    ?? defaultOrigins;

var allowedOrigins = corsOrigins
    .Split(
        ',',
        StringSplitOptions.RemoveEmptyEntries
        | StringSplitOptions.TrimEntries
    )
    .Select(origin => origin.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy
                .SetIsOriginAllowed(origin => AdLocalAPI.Helpers.CorsSecurityPolicy.IsOriginAllowed(origin, allowedOrigins))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});



// ======================================================
// PIPELINE HTTP
// ======================================================


var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
});

// ======================================================
// SWAGGER
// ======================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AdLocalAPI V1");
        options.RoutePrefix = "swagger";
    });

    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
}
else
{
    app.MapGet("/", () => Results.Ok(new
    {
        mensaje = "AdLocal API Operativa",
        ambiente = app.Environment.EnvironmentName,
        fecha = DateTime.UtcNow
    }));
}

app.MapGet("/ping", () => Results.Ok(new
{
    mensaje = "API funcionando correctamente",
    ambiente = app.Environment.EnvironmentName,
    puerto = port,
    fecha = DateTime.Now
}));

app.UseCors("AllowFrontend");

app.UseGlobalExceptionHandler();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

