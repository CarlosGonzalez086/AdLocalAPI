using AdLocalAPI.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace AdLocalAPI.Tests;

public class ConfiguracionSeguraTests
{
    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("   ")]
    public void ConfiguracionObligatoria_NoAceptaValoresVacios(string? value)
    {
        var error = Assert.Throws<InvalidOperationException>(() => ConfiguracionSegura.Requerir(value, "TEST_KEY"));
        Assert.Contains("TEST_KEY", error.Message);
    }

    [Theory]
    [InlineData("Development", "synthetic-local")]
    [InlineData("Production", "synthetic-base")]
    public void ArchivoLocal_SoloSeCargaEnDesarrollo(string environment, string expected)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"adlocal-config-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(directory);
        try {
            File.WriteAllText(Path.Combine(directory, "appsettings.Local.json"), "{\"Phase1SyntheticSetting\":\"synthetic-local\"}");
            var config = new ConfigurationManager();
            config.SetBasePath(directory);
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Phase1SyntheticSetting"] = "synthetic-base" });
            var env = new Mock<IHostEnvironment>(); env.SetupGet(e => e.EnvironmentName).Returns(environment);
            ConfiguracionSegura.CargarLocal(config, env.Object, Array.Empty<string>());
            Assert.Equal(expected, config["Phase1SyntheticSetting"]);
            ConfiguracionSegura.CargarLocal(config, env.Object, new[] { "--Phase1SyntheticSetting=synthetic-argument" });
            Assert.Equal(environment == "Development" ? "synthetic-argument" : "synthetic-base", config["Phase1SyntheticSetting"]);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
