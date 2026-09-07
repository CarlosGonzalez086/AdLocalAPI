using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.Controllers;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Integration
{
    public class AuthAndRbacIntegrationTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private UsuarioService CrearUsuarioService(
            AppDbContext context,
            DefaultHttpContext httpContext,
            IConfiguration? config = null,
            IRefreshTokenService? refreshTokenService = null,
            Mock<IUsuarioRepository>? userRepoMock = null)
        {
            var userRepo = userRepoMock ?? new Mock<IUsuarioRepository>();
            if (userRepoMock == null)
            {
                userRepo.Setup(r => r.GetByCorreoAsync(It.IsAny<string>()))
                    .ReturnsAsync((string correo) =>
                    {
                        var norm = correo.Trim().ToLowerInvariant();
                        return context.Usuarios.FirstOrDefault(u => u.Email.ToLower() == norm);
                    });
                userRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>()))
                    .ReturnsAsync((long id) => context.Usuarios.Find(id));
                userRepo.Setup(r => r.UpdateAsync(It.IsAny<Usuario>()))
                    .Returns<Usuario>(async u =>
                    {
                        context.Usuarios.Update(u);
                        await context.SaveChangesAsync();
                    });
                userRepo.Setup(r => r.CreateAsync(It.IsAny<Usuario>()))
                    .Returns<Usuario>(async u =>
                    {
                        await context.Usuarios.AddAsync(u);
                        await context.SaveChangesAsync();
                        return u;
                    });
            }

            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            var jwtContext = new JwtContext(accessor);

            var inMemoryConfig = config ?? new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "SuperSecretKeyForIntegrationTestingAdLocalApi2026!",
                    ["Jwt:Issuer"] = "AdLocalTestIssuer",
                    ["Jwt:Audience"] = "AdLocalTestAudience",
                    ["Jwt:ExpireMinutes"] = "60"
                })
                .Build();

            var rfService = refreshTokenService ?? new RefreshTokenService(
                new RefreshTokenRepository(context),
                NullLogger<RefreshTokenService>.Instance
            );

            return new UsuarioService(
                userRepo.Object,
                inMemoryConfig,
                jwtContext,
                new Mock<IComercioRepository>().Object,
                new Mock<IEmailService>().Object,
                new Mock<IWebHostEnvironment>().Object,
                new Mock<ISuscripcionRepository>().Object,
                new Mock<IPlanRepository>().Object,
                new Mock<IUsoCodigoReferidoRepository>().Object,
                rfService,
                accessor
            );
        }

        // ==============================================================
        // 1. REGISTRO Y VERIFICACIÓN DE CORREO
        // ==============================================================

        [Fact]
        public async Task RegistroYVerificacion_FlujoCompleto_VerificaExitosamente()
        {
            using var context = CreateDbContext();
            var httpContext = new DefaultHttpContext();
            var service = CrearUsuarioService(context, httpContext);

            var passwordPlano = "PasswordSeguro123!";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(passwordPlano);

            var usuario = new Usuario
            {
                Id = 1,
                Nombre = "Carlos González",
                Email = "carlos.verif@adlocal.com",
                PasswordHash = passwordHash,
                Rol = RolesUsuario.Cliente,
                Activo = true,
                EmailVerificado = false,
                Codigo = "654321",
                CodigoExpiracion = DateTime.UtcNow.AddHours(2),
                IntentosCodigo = 0
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            // Verificar correo con el código correcto
            var resultadoVerif = await service.VerificarCorreoAsync(new VerificarCorreoDto
            {
                Email = "carlos.verif@adlocal.com",
                Codigo = "654321"
            });

            Assert.Equal("200", resultadoVerif.Codigo);

            var usuarioActualizado = await context.Usuarios.FindAsync(1L);
            Assert.NotNull(usuarioActualizado);
            Assert.True(usuarioActualizado!.EmailVerificado);
            Assert.Null(usuarioActualizado.Codigo);
            Assert.Null(usuarioActualizado.CodigoExpiracion);
            Assert.Equal(0, usuarioActualizado.IntentosCodigo);
        }

        [Fact]
        public async Task VerificacionCorreo_AtaqueFuerzaBruta_BloqueaAlQuintoIntento()
        {
            using var context = CreateDbContext();
            var httpContext = new DefaultHttpContext();
            var service = CrearUsuarioService(context, httpContext);

            var usuario = new Usuario
            {
                Id = 2,
                Nombre = "Usuario Test",
                Email = "fuerzabruta@adlocal.com",
                PasswordHash = "hash123",
                Rol = RolesUsuario.Cliente,
                Activo = true,
                EmailVerificado = false,
                Codigo = "123456",
                CodigoExpiracion = DateTime.UtcNow.AddHours(1),
                IntentosCodigo = 4 // Ya falló 4 veces
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            // 5º intento fallido
            var intento5 = await service.VerificarCorreoAsync(new VerificarCorreoDto
            {
                Email = "fuerzabruta@adlocal.com",
                Codigo = "999999" // Código incorrecto
            });

            Assert.Equal("400", intento5.Codigo);
            Assert.Contains("máximo de intentos", intento5.Mensaje);

            var usuarioBloqueado = await context.Usuarios.FindAsync(2L);
            Assert.NotNull(usuarioBloqueado);
            Assert.Null(usuarioBloqueado!.Codigo);
            Assert.Null(usuarioBloqueado.CodigoExpiracion);
            Assert.False(usuarioBloqueado.EmailVerificado);
        }

        // ==============================================================
        // 2. LOGIN Y GENERACIÓN DE REFRESH TOKEN
        // ==============================================================

        [Fact]
        public async Task Login_CredencialesCorrectas_GeneraTokenYPersisteRefreshToken()
        {
            using var context = CreateDbContext();
            var httpContext = new DefaultHttpContext();
            var rfRepo = new RefreshTokenRepository(context);
            var rfService = new RefreshTokenService(rfRepo, NullLogger<RefreshTokenService>.Instance);
            var service = CrearUsuarioService(context, httpContext, refreshTokenService: rfService);

            var password = "MiPasswordValido2026!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            var usuario = new Usuario
            {
                Id = 10,
                Nombre = "Comercio Prueba",
                Email = "comercio@adlocal.com",
                PasswordHash = hash,
                Rol = RolesUsuario.Comercio,
                Activo = true,
                EmailVerificado = true
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            var resultadoLogin = await service.Login("comercio@adlocal.com", password);

            Assert.Equal("200", resultadoLogin.Codigo);
            Assert.NotNull(resultadoLogin.Respuesta);

            // Verificar que el RefreshToken se guardó en la base de datos
            var tokensGuardados = await context.RefreshTokens.Where(t => t.UsuarioId == 10).ToListAsync();
            Assert.Single(tokensGuardados);
            Assert.True(tokensGuardados[0].IsActive);
        }

        [Fact]
        public async Task Login_CuentaInactiva_Retorna403()
        {
            using var context = CreateDbContext();
            var httpContext = new DefaultHttpContext();
            var service = CrearUsuarioService(context, httpContext);

            var password = "Password123!";
            var usuario = new Usuario
            {
                Id = 11,
                Nombre = "Inactivo Bloqueado",
                Email = "inactivo@adlocal.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Rol = RolesUsuario.Cliente,
                Activo = false // Cuenta bloqueada
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            var resultado = await service.Login("inactivo@adlocal.com", password);

            Assert.Equal("403", resultado.Codigo);
            Assert.Contains("inactiva o bloqueada", resultado.Mensaje);
        }

        // ==============================================================
        // 3. DETECCIÓN DE REUSO DE REFRESH TOKEN (DEFENSA ROBO)
        // ==============================================================

        [Fact]
        public async Task RefreshToken_ReusoDeTokenRevocado_RevocaTodasLasSesionesDelUsuario()
        {
            using var context = CreateDbContext();
            var rfRepo = new RefreshTokenRepository(context);
            var rfService = new RefreshTokenService(rfRepo, NullLogger<RefreshTokenService>.Instance);

            var usuario = new Usuario
            {
                Id = 20,
                Nombre = "Seguridad Usuario",
                Email = "seguridad@adlocal.com",
                PasswordHash = "hash",
                Rol = RolesUsuario.Cliente,
                Activo = true
            };
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            // 1. Generar token original
            var (token1, _) = await rfService.GenerarRefreshTokenAsync(20);

            // 2. Rotar token legítimamente -> token1 queda revocado y se genera token2
            var rotacion1 = await rfService.RotarRefreshTokenAsync(token1);
            Assert.True(rotacion1.success);

            // 3. Atacante intenta reutilizar token1 (ya revocado)
            var ataque = await rfService.RotarRefreshTokenAsync(token1);

            Assert.False(ataque.success);
            Assert.Contains("comprometida", ataque.error);

            // 4. Verificar que TODAS las sesiones del usuario fueron invalidadas en BD
            var sesiones = await context.RefreshTokens.Where(t => t.UsuarioId == 20).ToListAsync();
            Assert.All(sesiones, s => Assert.False(s.IsActive));

            var usuarioDb = await context.Usuarios.FindAsync(20L);
            Assert.NotNull(usuarioDb);
            Assert.NotNull(usuarioDb!.TokensRevocadosAntesDe);
        }

        // ==============================================================
        // 4. CONTROL DE ACCESO RBAC EN CONTROLADORES
        // ==============================================================

        [Theory]
        [InlineData(typeof(AdminComisionesController), "Admin")]
        [InlineData(typeof(CuentasBancariasAdLocalController), "Admin,Comercio")]
        [InlineData(typeof(PedidosComercioController), "Comercio,Colaborador")]
        public void Controladores_AtributosAuthorizeYRoles_DefinidosCorrectamente(Type controllerType, string rolesEsperados)
        {
            var controllerAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
            var actionsWithAuthorize = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => m.GetCustomAttribute<AuthorizeAttribute>())
                .Where(a => a != null)
                .ToList();

            var rolesEncontrados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(controllerAttr?.Roles))
            {
                foreach (var r in controllerAttr.Roles.Split(','))
                    rolesEncontrados.Add(r.Trim());
            }

            foreach (var actionAttr in actionsWithAuthorize)
            {
                if (!string.IsNullOrEmpty(actionAttr?.Roles))
                {
                    foreach (var r in actionAttr.Roles.Split(','))
                        rolesEncontrados.Add(r.Trim());
                }
            }

            var esperados = rolesEsperados.Split(',').Select(r => r.Trim());
            foreach (var rol in esperados)
            {
                Assert.Contains(rol, rolesEncontrados);
            }
        }

        [Fact]
        public async Task AuthController_Sesiones_UsuarioNoAutenticado_Retorna401Unauthorized()
        {
            var userRepoMock = new Mock<IUsuarioService>();
            var controller = new AuthController(userRepoMock.Object);

            var httpContext = new DefaultHttpContext();
            // Sin ClaimsPrincipal autenticado
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var rfMock = new Mock<IRefreshTokenService>();
            var resultado = await controller.ObtenerSesiones(rfMock.Object);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(resultado);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
        }
    }
}
