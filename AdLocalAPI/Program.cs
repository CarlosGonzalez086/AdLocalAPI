using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Repositories;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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

// Stripe
var stripeSecret = Environment.GetEnvironmentVariable("STRIPE__SecretKey");
if (!string.IsNullOrEmpty(stripeSecret))
{
    StripeConfiguration.ApiKey = stripeSecret;
}

// PostgreSQL / Supabase
var connectionString = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION")
    ?? throw new Exception("❌ SUPABASE_DB_CONNECTION no está definida");

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
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 1,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null
            );
        });
});

// ======================================================
// SERVICIOS Y REPOSITORIOS
// ======================================================

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<JwtContext>();

builder.Services.AddScoped<ComercioRepository>();
builder.Services.AddScoped<ComercioService>();

builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();

builder.Services.AddScoped<PlanRepository>();
builder.Services.AddScoped<AdLocalAPI.Services.PlanService>();

builder.Services.AddScoped<SuscripcionRepository>();

builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();

// ======================================================
// CONTROLLERS + SWAGGER
// ======================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ======================================================
// CORS
// ======================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
            "http://localhost:5173",
            "https://ad-local-delta.vercel.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
    );
});

// ======================================================
// PIPELINE HTTP
// ======================================================

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
});

app.UseAuthentication(); // 🔑 JWT
app.UseAuthorization();

app.MapControllers();
app.Run();
