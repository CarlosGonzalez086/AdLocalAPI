using AdLocalAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace AdLocalAPI.Data;

// Sustituye tu fábrica de diseño existente por esta implementación.
// No mantengas dos fábricas para el mismo DbContext.
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // La misma prioridad de conexión que en Program.cs.
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No se encontró la conexión PostgreSQL para migraciones. " +
                "Configura DB_CONNECTION o ConnectionStrings:DefaultConnection " +
                "en appsettings.Local.json o en variables de entorno."
            );
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseNetTopologySuite();
                npgsql.CommandTimeout(30);
                npgsql.ExecutionStrategy(deps =>
                    new NonRetryingExecutionStrategy(deps));
            })
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        return new AppDbContext(options);
    }
}
