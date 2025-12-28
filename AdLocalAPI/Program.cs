using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Repositories;
using AdLocalAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT
var key = builder.Configuration["Jwt:Key"] ?? "ClaveSuperSecreta123!";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "AdLocalAPI";

// Stripe
var stripeSecret = builder.Configuration["Stripe:SecretKey"];
StripeConfiguration.ApiKey = stripeSecret;

// AUTH
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<JwtContext>();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repos & Services
builder.Services.AddScoped<ComercioRepository>();
builder.Services.AddScoped<ComercioService>();

builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();

builder.Services.AddScoped<PlanRepository>();
builder.Services.AddScoped<AdLocalAPI.Services.PlanService>();

builder.Services.AddScoped<SuscripcionRepository>();

builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();


// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
            "http://localhost:5173",
            "https://localhost:5173",
            "https://tudominio.com"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
    );
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.UseAuthentication(); // 🔑 CLAVE
app.UseAuthorization();

app.MapControllers();
app.Run();
