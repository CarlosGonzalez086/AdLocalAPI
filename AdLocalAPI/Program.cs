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

var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE__URL")
    ?? "https://uzgnfwbztoizcctyfdiv.supabase.co";

var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE__KEY")
    ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV6Z25md2J6dG9pemNjdHlmZGl2Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2Njk0MzUyNywiZXhwIjoyMDgyNTE5NTI3fQ.opjCm_q7U9GX0ah7UUgRMzQJwBQhyBupWVGJQXY6v0I";

// PostgreSQL / Supabase
var connectionString = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION")
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

builder.Services.AddDbContextPool<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.UseNetTopologySuite();
        npgsql.CommandTimeout(30);
        // ❌ NO retries automáticos en Postgres
    });

    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
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

