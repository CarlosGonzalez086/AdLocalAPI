using System;
using AdLocalAPI.Data;
using AdLocalAPI.Interfaces.ProductosServicios;
using AdLocalAPI.Interfaces.Repository;
using AdLocalAPI.Repositories;
using AdLocalAPI.Repositories.Interfaces;
using Amazon.S3;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class DependencyInjectionResolutionTests
    {
        [Fact]
        public void Repositories_CanBeResolved_WithoutSupabaseClient()
        {
            var services = new ServiceCollection();

            // Mock infrastructure dependencies
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            services.AddScoped(_ => new AppDbContext(options));
            services.AddSingleton(new Mock<IWebHostEnvironment>().Object);
            services.AddSingleton(new Mock<IAmazonS3>().Object);

            // Register repositories matching Program.cs
            services.AddScoped<IComercioRepository, ComercioRepository>();
            services.AddScoped<ComercioRepository>();

            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<UsuarioRepository>();

            services.AddScoped<IProductosServiciosRepository, ProductosServiciosRepository>();

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            // Act & Assert: these should resolve cleanly without any Supabase.Client dependency
            var comercioRepo = scope.ServiceProvider.GetRequiredService<IComercioRepository>();
            Assert.NotNull(comercioRepo);

            var comercioRepoDirect = scope.ServiceProvider.GetRequiredService<ComercioRepository>();
            Assert.NotNull(comercioRepoDirect);

            var usuarioRepo = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
            Assert.NotNull(usuarioRepo);

            var usuarioRepoDirect = scope.ServiceProvider.GetRequiredService<UsuarioRepository>();
            Assert.NotNull(usuarioRepoDirect);

            var prodRepo = scope.ServiceProvider.GetRequiredService<IProductosServiciosRepository>();
            Assert.NotNull(prodRepo);
        }
    }
}
