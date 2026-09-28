using System;
using System.Threading.Tasks;
using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class EmailVerificationTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private class FakeClienteRepository : IClienteRepository
        {
            public Usuario? Usuario { get; set; }

            public Task<Usuario?> ObtenerPorEmailAsync(string email)
            {
                if (Usuario != null && string.Equals(Usuario.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<Usuario?>(Usuario);
                }
                return Task.FromResult<Usuario?>(null);
            }

            public Task<Usuario?> ObtenerPorIdAsync(long id)
            {
                return Task.FromResult(Usuario?.Id == id ? Usuario : null);
            }

            public Task<bool> ExisteEmailAsync(string email)
            {
                return Task.FromResult(Usuario != null && string.Equals(Usuario.Email, email, StringComparison.OrdinalIgnoreCase));
            }

            public Task<Usuario> CrearAsync(Usuario usuario)
            {
                Usuario = usuario;
                return Task.FromResult(usuario);
            }

            public Task ActualizarAsync(Usuario usuario)
            {
                Usuario = usuario;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task UsuarioService_VerificarCorreoAsync_ValidCode_MarksEmailVerified()
        {
            // Arrange
            using var context = CreateDbContext();
            var repo = new UsuarioRepository(context, null!, null!, null!);
            var service = new UsuarioService(repo, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

            var user = new Usuario
            {
                Id = 100,
                Email = "verif@example.com",
                Nombre = "Test Verif",
                PasswordHash = "h",
                Rol = "Comercio",
                Activo = true,
                EmailVerificado = false,
                Codigo = "123456",
                CodigoExpiracion = DateTime.UtcNow.AddMinutes(15),
                IntentosCodigo = 0
            };
            context.Usuarios.Add(user);
            await context.SaveChangesAsync();

            // Act
            var response = await service.VerificarCorreoAsync(new VerificarCorreoDto
            {
                Email = "verif@example.com",
                Codigo = "123456"
            });

            // Assert
            Assert.Equal("200", response.Codigo);
            var updated = await context.Usuarios.FindAsync(user.Id);
            Assert.NotNull(updated);
            Assert.True(updated.EmailVerificado);
            Assert.Null(updated.Codigo);
            Assert.Null(updated.CodigoExpiracion);
            Assert.Equal(0, updated.IntentosCodigo);
        }

        [Fact]
        public async Task UsuarioService_VerificarCorreoAsync_ExpiredCode_Rejects()
        {
            // Arrange
            using var context = CreateDbContext();
            var repo = new UsuarioRepository(context, null!, null!, null!);
            var service = new UsuarioService(repo, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

            var user = new Usuario
            {
                Id = 101,
                Email = "expirado@example.com",
                Nombre = "Test Exp",
                PasswordHash = "h",
                Rol = "Comercio",
                Activo = true,
                EmailVerificado = false,
                Codigo = "123456",
                CodigoExpiracion = DateTime.UtcNow.AddMinutes(-5), // Expirado hace 5 min
                IntentosCodigo = 0
            };
            context.Usuarios.Add(user);
            await context.SaveChangesAsync();

            // Act
            var response = await service.VerificarCorreoAsync(new VerificarCorreoDto
            {
                Email = "expirado@example.com",
                Codigo = "123456"
            });

            // Assert
            Assert.NotEqual("200", response.Codigo);
            Assert.Contains("ha expirado", response.Mensaje, StringComparison.OrdinalIgnoreCase);

            var updated = await context.Usuarios.FindAsync(user.Id);
            Assert.NotNull(updated);
            Assert.False(updated.EmailVerificado);
        }

        [Fact]
        public async Task UsuarioService_VerificarCorreoAsync_InvalidCode_IncrementsAttempts()
        {
            // Arrange
            using var context = CreateDbContext();
            var repo = new UsuarioRepository(context, null!, null!, null!);
            var service = new UsuarioService(repo, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

            var user = new Usuario
            {
                Id = 102,
                Email = "intentos@example.com",
                Nombre = "Test Intentos",
                PasswordHash = "h",
                Rol = "Comercio",
                Activo = true,
                EmailVerificado = false,
                Codigo = "123456",
                CodigoExpiracion = DateTime.UtcNow.AddMinutes(15),
                IntentosCodigo = 0
            };
            context.Usuarios.Add(user);
            await context.SaveChangesAsync();

            // Act: Intentar con código erróneo
            var response = await service.VerificarCorreoAsync(new VerificarCorreoDto
            {
                Email = "intentos@example.com",
                Codigo = "999999"
            });

            // Assert
            Assert.NotEqual("200", response.Codigo);
            Assert.Contains("Intentos restantes: 4", response.Mensaje);

            var updated = await context.Usuarios.FindAsync(user.Id);
            Assert.NotNull(updated);
            Assert.Equal(1, updated.IntentosCodigo);
            Assert.False(updated.EmailVerificado);
        }

        [Fact]
        public async Task UsuarioService_VerificarCorreoAsync_FifthFailedAttempt_LocksOutAndWipesCode()
        {
            // Arrange
            using var context = CreateDbContext();
            var repo = new UsuarioRepository(context, null!, null!, null!);
            var service = new UsuarioService(repo, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

            var user = new Usuario
            {
                Id = 103,
                Email = "lockout@example.com",
                Nombre = "Test Lockout",
                PasswordHash = "h",
                Rol = "Comercio",
                Activo = true,
                EmailVerificado = false,
                Codigo = "123456",
                CodigoExpiracion = DateTime.UtcNow.AddMinutes(15),
                IntentosCodigo = 4 // Ya van 4 intentos fallidos
            };
            context.Usuarios.Add(user);
            await context.SaveChangesAsync();

            // Act: 5º intento fallido
            var response = await service.VerificarCorreoAsync(new VerificarCorreoDto
            {
                Email = "lockout@example.com",
                Codigo = "000000"
            });

            // Assert
            Assert.NotEqual("200", response.Codigo);
            Assert.Contains("superado el número máximo de intentos", response.Mensaje, StringComparison.OrdinalIgnoreCase);

            var updated = await context.Usuarios.FindAsync(user.Id);
            Assert.NotNull(updated);
            Assert.Null(updated.Codigo); // Código invalidado
            Assert.Null(updated.CodigoExpiracion);
            Assert.Equal(0, updated.IntentosCodigo);
            Assert.False(updated.EmailVerificado);
        }

        [Fact]
        public async Task ClienteService_VerificarCorreoAsync_ValidFlow_And_LockoutDefense()
        {
            // Arrange
            var user = new Usuario
            {
                Id = 200,
                Email = "cliente@example.com",
                Nombre = "Cliente Test",
                PasswordHash = "h",
                Rol = "Cliente",
                Activo = true,
                EmailVerificado = false,
                Codigo = "654321",
                CodigoExpiracion = DateTime.UtcNow.AddMinutes(15),
                IntentosCodigo = 0
            };

            var fakeRepo = new FakeClienteRepository { Usuario = user };
            var service = new ClienteService(fakeRepo, null!, null!, null!, null!, null!, null!);

            // Act 1: Intento fallido
            var resFail = await service.VerificarCorreoAsync(new VerificarCorreoDto
            {
                Email = "cliente@example.com",
                Codigo = "111111"
            });
            Assert.NotEqual("200", resFail.Codigo);
            Assert.Equal(1, user.IntentosCodigo);

            // Act 2: Código correcto después de un intento fallido
            var resSuccess = await service.VerificarCorreoAsync(new VerificarCorreoDto
            {
                Email = "cliente@example.com",
                Codigo = "654321"
            });

            // Assert 2
            Assert.Equal("200", resSuccess.Codigo);
            Assert.True(user.EmailVerificado);
            Assert.Null(user.Codigo);
            Assert.Null(user.CodigoExpiracion);
            Assert.Equal(0, user.IntentosCodigo);

            // Act 3: Intentar verificar nuevamente cuando ya está verificado
            var resAlready = await service.VerificarCorreoAsync(new VerificarCorreoDto
            {
                Email = "cliente@example.com",
                Codigo = "654321"
            });
            Assert.Equal("200", resAlready.Codigo);
            Assert.Contains("ya se encuentra verificado", resAlready.Mensaje, StringComparison.OrdinalIgnoreCase);
        }
    }
}
