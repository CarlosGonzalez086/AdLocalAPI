using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Interfaces.Tarjetas;
using AdLocalAPI.Repositories;
using AdLocalAPI.Services;
using AdLocalAPI.Utils;
using AdLocalAPI.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using System.Text;

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
    ?? throw new Exception("❌ JWT__Key no está definido");

var jwtIssuer = Environment.GetEnvironmentVariable("JWT__Issuer")
    ?? "AdLocalAPI";

//var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");

//if (string.IsNullOrWhiteSpace(webhookSecret))
//{
//    throw new Exception("Stripe Webhook Secret no configurado");
//}

// Stripe
//var stripeSecret = Environment.GetEnvironmentVariable("STRIPE__SecretKey");
//if (!string.IsNullOrEmpty(stripeSecret))
//{
//    StripeConfiguration.ApiKey = stripeSecret;
//}

var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE__URL")
    ?? "https://uzgnfwbztoizcctyfdiv.supabase.co";

var supabaseKey =  "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV6Z25md2J6dG9pemNjdHlmZGl2Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2Njk0MzUyNywiZXhwIjoyMDgyNTE5NTI3fQ.opjCm_q7U9GX0ah7UUgRMzQJwBQhyBupWVGJQXY6v0I";

// PostgreSQL / Supabase
var connectionString = Environment
    .GetEnvironmentVariable("SUPABASE_DB_CONNECTION")
    ?.Trim()
    ?? throw new Exception("❌ SUPABASE_DB_CONNECTION no está definida");

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
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        )
    };
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
            npgsql.UseNetTopologySuite(); // ✅ se queda
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

builder.Services.AddScoped<ComercioRepository>();
builder.Services.AddScoped<ComercioService>();

builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();

builder.Services.AddScoped<IProductosServiciosRepository, ProductosServiciosRepository>();
builder.Services.AddScoped<IProductosServiciosService, ProductosServiciosService>();

builder.Services.AddScoped<PlanRepository>();
builder.Services.AddScoped<AdLocalAPI.Services.PlanService>();

builder.Services.AddScoped<SuscripcionRepository>();
builder.Services.AddScoped<SuscripcionService>();

builder.Services.AddScoped<StripeService>();

builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();

builder.Services.AddScoped<ITarjetaService, TarjetaService>();
builder.Services.AddScoped<ITarjetaRepository, TarjetaRepository>();
builder.Services.AddScoped<IStripeService, StripeService>();


builder.Services.AddSingleton(new Supabase.Client(supabaseUrl, supabaseKey));

// ======================================================
// CONTROLLERS + SWAGGER
// ======================================================

builder.Services.AddControllers();
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
            "http://localhost:5173",
            "https://ad-local-gamma.vercel.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
    );
});



// ======================================================
// PIPELINE HTTP
// ======================================================


var app = builder.Build();


//Asignacion de la variable singleton

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var stripeSettings = scope.ServiceProvider.GetRequiredService<StripeSettings>();

    var secretKey = await dbContext.ConfiguracionSistema
        .Where(c => c.Key == "STRIPE_SECRET_KEY")
        .Select(c => c.Val)
        .FirstOrDefaultAsync();

    stripeSettings.Inicializar(secretKey ?? "sk_test_default_value");
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AdLocalAPI V1");
    options.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

