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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

//Singlenton variable
builder.Services.AddSingleton<StripeSettings>();

// ======================================================
// VARIABLES DE ENTORNO (Docker / Producción / Local)
// ======================================================

// Puerto (Railway / Docker)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// JWT
var jwtKey = Environment.GetEnvironmentVariable("JWT__Key")
    ?? builder.Configuration["Jwt:Key"]
    ?? throw new Exception("❌ JWT__Key no está definido");

var jwtIssuer = Environment.GetEnvironmentVariable("JWT__Issuer")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "AdLocalAPI";

var jwtAudience = Environment.GetEnvironmentVariable("JWT__Audience")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "AdLocal";

var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");

if (string.IsNullOrWhiteSpace(webhookSecret))
{
    throw new Exception("Stripe Webhook Secret no configurado");
}

var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE__URL")
    ?? "https://uzgnfwbztoizcctyfdiv.supabase.co";

var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV6Z25md2J6dG9pemNjdHlmZGl2Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2Njk0MzUyNywiZXhwIjoyMDgyNTE5NTI3fQ.opjCm_q7U9GX0ah7UUgRMzQJwBQhyBupWVGJQXY6v0I";

// PostgreSQL / Supabase
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("No se encontró cadena de conexión");

Stripe.StripeConfiguration.ApiKey =
   builder.Configuration["Stripe:SecretKey"];

//var connectionString = "User Id=postgres.uzgnfwbztoizcctyfdiv;Password=q8dZ1szsEYIOzKrM;Server=aws-1-us-east-2.pooler.supabase.com;Port=6543;Database=postgres;SSL Mode=Require;Trust Server Certificate=true";

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

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var userIdClaim = context.Principal?.FindFirst("id")?.Value
                ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (long.TryParse(userIdClaim, out var userId))
            {
                var user = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                var (isValid, errorMessage) = AdLocalAPI.Helpers.TokenSecurityValidator.ValidarUsuarioYClaims(user, context.Principal);
                if (!isValid)
                {
                    context.Fail(errorMessage!);
                    return;
                }
            }
        }
    };
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
builder.Services.AddScoped<IGeoLocationService, GeoLocationService>();
builder.Services.AddScoped<GeoLocationService>();
builder.Services.AddScoped<HttpClient>();

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

builder.Services.AddScoped<IPedidoRepository,PedidoRepository>();
builder.Services.AddScoped<ICheckoutService,AdLocalAPI.Services.CheckoutService>();
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

builder.Services.AddScoped<ICitaService,CitaService>();
builder.Services.AddScoped<ICitaRepository,CitaRepository>();
builder.Services.AddScoped<IHorarioCitaServicioRepository,HorarioCitaServicioRepository>();

builder.Services.AddScoped<ICotizacionRepository, CotizacionRepository>();
builder.Services.AddScoped<ICotizacionService, CotizacionService>();

builder.Services.AddScoped<ICuentaBancariaAdLocalRepository, CuentaBancariaAdLocalRepository>();
builder.Services.AddScoped<ICuentaBancariaAdLocalService, CuentaBancariaAdLocalService>();

builder.Services.AddScoped<IPagoComisionRepository, PagoComisionRepository>();
builder.Services.AddScoped<IPagoComisionService, PagoComisionService>();

builder.Services.AddScoped<IStripeWebhookEventRepository, StripeWebhookEventRepository>();
builder.Services.AddScoped<IWebhookService, WebhookService>();




builder.Services.AddSingleton<AppConfigState>();




builder.Services.AddSingleton(new Supabase.Client(supabaseUrl, supabaseKey));

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


//using (var scope = app.Services.CreateScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    var stripeSettings = scope.ServiceProvider.GetRequiredService<StripeSettings>();

//    var secretKey = await dbContext.ConfiguracionSistema
//        .Where(c => c.Key == "STRIPE_SECRET_KEY")
//        .Select(c => c.Val)
//        .FirstOrDefaultAsync();

//    stripeSettings.Inicializar(secretKey ?? "sk_test_default_value");
//}

//using (var scope = app.Services.CreateScope())
//{
//    var repo = scope.ServiceProvider
//        .GetRequiredService<IConfiguracionRepository>();

//    var provider = scope.ServiceProvider
//        .GetRequiredService<StripeConfigProvider>();

//    var configs = await repo.ObtenerTodosAsync();

//    provider.Load(configs);

//    Stripe.StripeConfiguration.ApiKey = provider.SecretKey;

//    Console.WriteLine("Stripe loaded from DB: " +
//        (provider.SecretKey.StartsWith("sk_live") ? "LIVE" : "TEST"));
//}

//using (var scope = app.Services.CreateScope())
//{
//    var repo = scope.ServiceProvider
//        .GetRequiredService<IConfiguracionRepository>();

//    var provider = scope.ServiceProvider
//        .GetRequiredService<ClavesConfigProvider>();

//    var appConfig = scope.ServiceProvider
//        .GetRequiredService<AppConfigState>();

//    var configs = await repo.ObtenerTodosAsync();

//    provider.Load(configs);

//    appConfig.SetIp2LocationKey(provider.Ip2LocationKey);

//    Console.WriteLine(
//        "Ip2LocationKey loaded from DB: " + appConfig.Ip2LocationKey
//    );
//}


app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
});

// ======================================================
// SWAGGER
// ======================================================

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AdLocalAPI V1");
    options.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger/index.html"));

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

