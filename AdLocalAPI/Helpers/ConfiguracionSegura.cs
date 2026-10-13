namespace AdLocalAPI.Helpers;

public static class ConfiguracionSegura
{
    public static void CargarLocal(ConfigurationManager configuration, IHostEnvironment environment, string[] args)
    {
        if (!environment.IsDevelopment()) return;
        // Entorno y argumentos conservan precedencia sobre el archivo privado local.
        configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();
        if (args.Length > 0) configuration.AddCommandLine(args);
    }

    public static string Requerir(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Falta la configuración obligatoria {name}.");
        return value;
    }
}
