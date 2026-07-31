using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.Comercio;
using AdLocalAPI.Interfaces.Location;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Interfaces.Tarjetas;
using AdLocalAPI.Interfaces.TipoComercio;
using AdLocalAPI.Repositories;
using AdLocalAPI.Services;
using AdLocalAPI.Utils;
using AdLocalAPI.Validators;
using Amazon.Runtime;
using Amazon.S3;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// CONFIGURACIÓN DEL SERVIDOR
// ======================================================

// Puerto utilizado por Railway, Render o Docker.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

builder.WebHost.UseUrls($"http://*:{port}");

// ======================================================
// VARIABLES DE ENTORNO
// ======================================================

// JWT
var jwtKey =
    Environment.GetEnvironmentVariable("JWT__Key")
    ?? throw new Exception("JWT__Key no está definido.");

var jwtIssuer =
    Environment.GetEnvironmentVariable("JWT__Issuer")
    ?? "AdLocalAPI";

// Stripe
var webhookSecret =
    Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");

if (string.IsNullOrWhiteSpace(webhookSecret))
{
    throw new Exception(
        "STRIPE_WEBHOOK_SECRET no está definido."
    );
}

// Supabase
//var supabaseUrl =
//    Environment.GetEnvironmentVariable("SUPABASE__URL")
//    ?? "https://uzgnfwbztoizcctyfdiv.supabase.co";

var supabaseKey =
    Environment.GetEnvironmentVariable(
        "SUPABASE__SERVICE_ROLE_KEY"
    )
    ?? throw new Exception(
        "SUPABASE__SERVICE_ROLE_KEY no está definida."
    );

// PostgreSQL
var connectionString =
    Environment
        .GetEnvironmentVariable(
            "SUPABASE_DB_CONNECTION"
        )
        ?.Trim()
    ?? throw new Exception(
        "SUPABASE_DB_CONNECTION no está definida."
    );

// ======================================================
// ORÍGENES PERMITIDOS POR CORS
// ======================================================

// Puedes sobrescribir estos dominios con la variable:
// CORS__ALLOWED_ORIGINS
//
// Ejemplo:
// https://adlocal.store,https://www.adlocal.store

var defaultOrigins = string.Join(
    ",",
    "http://localhost:4321",
    "http://127.0.0.1:4321",
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "https://adlocal.store",
    "https://www.adlocal.store",
    "https://ad-local-gamma.vercel.app",
    "https://adlocalweb.jcarlosgonzalez086.workers.dev"
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

// ======================================================
// CONFIGURACIÓN DE STRIPE
// ======================================================

builder.Services.AddSingleton<StripeSettings>();

var initialStripeSecretKey =
    builder.Configuration["Stripe:SecretKey"]
    ?? Environment.GetEnvironmentVariable(
        "STRIPE__SECRET_KEY"
    );

if (!string.IsNullOrWhiteSpace(initialStripeSecretKey))
{
    StripeConfiguration.ApiKey =
        initialStripeSecretKey;
}

// ======================================================
// AUTENTICACIÓN JWT
// ======================================================

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme
    )
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

// ======================================================
// CLOUDFLARE R2
// ======================================================

builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var configuration = builder.Configuration;

    var accessKey =
        configuration["R2:AccessKeyId"]
        ?? Environment.GetEnvironmentVariable(
            "R2__ACCESS_KEY_ID"
        );

    var secretKey =
        configuration["R2:SecretAccessKey"]
        ?? Environment.GetEnvironmentVariable(
            "R2__SECRET_ACCESS_KEY"
        );

    var accountId =
        configuration["R2:AccountId"]
        ?? Environment.GetEnvironmentVariable(
            "R2__ACCOUNT_ID"
        );

    if (string.IsNullOrWhiteSpace(accessKey))
    {
        throw new Exception(
            "La clave R2 AccessKeyId no está configurada."
        );
    }

    if (string.IsNullOrWhiteSpace(secretKey))
    {
        throw new Exception(
            "La clave R2 SecretAccessKey no está configurada."
        );
    }

    if (string.IsNullOrWhiteSpace(accountId))
    {
        throw new Exception(
            "La variable R2 AccountId no está configurada."
        );
    }

    var credentials =
        new BasicAWSCredentials(
            accessKey,
            secretKey
        );

    var s3Config = new AmazonS3Config
    {
        ServiceURL =
            $"https://{accountId}.r2.cloudflarestorage.com",

        AuthenticationRegion = "auto",
        ForcePathStyle = true,
        UseHttp = false,
        MaxErrorRetry = 5
    };

    return new AmazonS3Client(
        credentials,
        s3Config
    );
});

// ======================================================
// ENTITY FRAMEWORK CORE
// ======================================================

builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.UseNetTopologySuite();
                npgsql.CommandTimeout(30);

                npgsql.ExecutionStrategy(
                    dependencies =>
                        new NonRetryingExecutionStrategy(
                            dependencies
                        )
                );
            }
        );

        options.UseQueryTrackingBehavior(
            QueryTrackingBehavior.NoTracking
        );
    }
);

// ======================================================
// FLUENT VALIDATION
// ======================================================

builder.Services
    .AddValidatorsFromAssemblyContaining<
        ProductosServiciosDtoValidator
    >();

// ======================================================
// SERVICIOS GENERALES
// ======================================================

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient();

builder.Services.AddScoped<JwtContext>();

// ======================================================
// COMERCIOS
// ======================================================

builder.Services.AddScoped<ComercioRepository>();

builder.Services.AddScoped<ComercioService>();

builder.Services.AddScoped<
    IRelComercioImagenRepositorio,
    RelComercioImagenRepositorio
>();

// ======================================================
// USUARIOS
// ======================================================

builder.Services.AddScoped<UsuarioRepository>();

builder.Services.AddScoped<UsuarioService>();

// ======================================================
// PRODUCTOS Y SERVICIOS
// ======================================================

builder.Services.AddScoped<
    IProductosServiciosRepository,
    ProductosServiciosRepository
>();

builder.Services.AddScoped<
    IProductosServiciosService,
    ProductosServiciosService
>();

builder.Services.AddScoped<
    IHorarioComercioService,
    HorarioComercioRepository
>();

// ======================================================
// PLANES
// ======================================================

builder.Services.AddScoped<PlanRepository>();

builder.Services.AddScoped<
    AdLocalAPI.Services.PlanService
>();

// ======================================================
// SUSCRIPCIONES
// ======================================================

builder.Services.AddScoped<SuscripcionRepository>();

builder.Services.AddScoped<SuscripcionService>();

builder.Services.AddScoped<
    ISuscriptionServiceV1,
    SuscriptionService
>();

builder.Services.AddScoped<
    ISuscriptionRepository,
    SuscriptionRepository
>();

// ======================================================
// STRIPE
// ======================================================

builder.Services.AddScoped<
    AdLocalAPI.Services.StripeService
>();

builder.Services.AddScoped<
    IStripeService,
    AdLocalAPI.Services.StripeService
>();

builder.Services.AddSingleton<
    StripeConfigProvider
>();

builder.Services.AddSingleton<
    ClavesConfigProvider
>();

// ======================================================
// GEOLOCALIZACIÓN
// ======================================================

builder.Services.AddScoped<GeoLocationService>();

builder.Services.AddScoped<
    ILocationRepository,
    LocationRepository
>();

builder.Services.AddScoped<
    ILocationService,
    LocationService
>();

// ======================================================
// CONFIGURACIONES
// ======================================================

builder.Services.AddScoped<
    IConfiguracionService,
    ConfiguracionService
>();

builder.Services.AddScoped<
    IConfiguracionRepository,
    ConfiguracionRepository
>();

builder.Services.AddSingleton<AppConfigState>();

// ======================================================
// TARJETAS
// ======================================================

builder.Services.AddScoped<
    ITarjetaService,
    TarjetaService
>();

builder.Services.AddScoped<
    ITarjetaRepository,
    TarjetaRepository
>();

// ======================================================
// TIPOS DE COMERCIO
// ======================================================

builder.Services.AddScoped<
    ITipoComercioRepository,
    TipoComercioRepository
>();

builder.Services.AddScoped<
    ITipoComercioService,
    TipoComercioService
>();

// ======================================================
// CALIFICACIONES Y COMENTARIOS
// ======================================================

builder.Services.AddScoped<
    CalificacionComentarioRepository
>();

builder.Services.AddScoped<
    CalificacionComentarioService
>();

// ======================================================
// CORREO ELECTRÓNICO
// ======================================================

builder.Services.Configure<EmailSettingsSendGrid>(
    builder.Configuration.GetSection(
        "EmailSettingsSendGrid"
    )
);

builder.Services.AddScoped<EmailService>();

// ======================================================
// VISITAS DE COMERCIOS
// ======================================================

builder.Services.AddScoped<
    ComercioVisitaRepository
>();

builder.Services.AddScoped<
    ComercioVisitaService
>();

// ======================================================
// CÓDIGOS REFERIDOS
// ======================================================

builder.Services.AddScoped<
    UsoCodigoReferidoRepository
>();

builder.Services.AddScoped<
    UsoCodigoReferidoService
>();

// ======================================================
// BENEFICIOS
// ======================================================

builder.Services.AddScoped<BeneficiosServices>();

// ======================================================
// SUPABASE CLIENT
// ======================================================

//builder.Services.AddSingleton(
//    new Supabase.Client(
//        supabaseUrl,
//        supabaseKey
//    )
//);

// ======================================================
// CONTROLADORES Y SWAGGER
// ======================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "AdLocal API",
            Version = "v1"
        }
    );
});

// ======================================================
// CORS
// ======================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

// ======================================================
// CONSTRUIR APLICACIÓN
// ======================================================

var app = builder.Build();

// ======================================================
// CARGAR CONFIGURACIÓN DE STRIPE DESDE LA BASE DE DATOS
// ======================================================

using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    var stripeSettings =
        scope.ServiceProvider
            .GetRequiredService<StripeSettings>();

    var secretKey = await dbContext
        .ConfiguracionSistema
        .Where(
            configuration =>
                configuration.Key
                == "STRIPE_SECRET_KEY"
        )
        .Select(
            configuration =>
                configuration.Val
        )
        .FirstOrDefaultAsync();

    stripeSettings.Inicializar(
        secretKey ?? "sk_test_default_value"
    );
}

// ======================================================
// CARGAR CONFIGURACIÓN GLOBAL DE STRIPE
// ======================================================

using (var scope = app.Services.CreateScope())
{
    var repository =
        scope.ServiceProvider
            .GetRequiredService<
                IConfiguracionRepository
            >();

    var provider =
        scope.ServiceProvider
            .GetRequiredService<
                StripeConfigProvider
            >();

    var configurations =
        await repository.ObtenerTodosAsync();

    provider.Load(configurations);

    if (!string.IsNullOrWhiteSpace(
        provider.SecretKey
    ))
    {
        StripeConfiguration.ApiKey =
            provider.SecretKey;

        var environment =
            provider.SecretKey.StartsWith(
                "sk_live",
                StringComparison.OrdinalIgnoreCase
            )
                ? "LIVE"
                : "TEST";

        Console.WriteLine(
            $"Stripe cargado desde la base de datos: {environment}"
        );
    }
    else
    {
        Console.WriteLine(
            "No se encontró una clave válida de Stripe."
        );
    }
}

// ======================================================
// CARGAR CLAVES DE CONFIGURACIÓN
// ======================================================

using (var scope = app.Services.CreateScope())
{
    var repository =
        scope.ServiceProvider
            .GetRequiredService<
                IConfiguracionRepository
            >();

    var provider =
        scope.ServiceProvider
            .GetRequiredService<
                ClavesConfigProvider
            >();

    var appConfig =
        scope.ServiceProvider
            .GetRequiredService<AppConfigState>();

    var configurations =
        await repository.ObtenerTodosAsync();

    provider.Load(configurations);

    appConfig.SetIp2LocationKey(
        provider.Ip2LocationKey
    );

    Console.WriteLine(
        "Configuración de geolocalización cargada."
    );
}

// ======================================================
// PIPELINE HTTP
// ======================================================

app.UseForwardedHeaders(
    new ForwardedHeadersOptions
    {
        ForwardedHeaders =
            ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
    }
);

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "AdLocalAPI V1"
    );

    options.RoutePrefix = "swagger";
});

app.UseRouting();

// Debe coincidir exactamente con el nombre registrado.
app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();