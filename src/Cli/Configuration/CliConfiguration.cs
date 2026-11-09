using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cli.Configuration;

internal sealed record CliConfiguration(AppOptions Options, string DataFile, bool HasApiKey)
{
    public static CliConfiguration Load(string[] args)
    {
        // Конфігураційні аргументи мають форму --Section:Key=value;
        // команди export/import та прапорці лабораторної 8 не передаємо провайдеру.
        var overrides = args.Where(a => a.StartsWith("--", StringComparison.Ordinal)
            && a.Contains('=') && a.Contains(':')).ToArray();
        var environment = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";
        if (environment is not ("Development" or "Production"))
            throw new ArgumentException("CROSSAPP_ENVIRONMENT: дозволено Development або Production.");
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: "CROSSAPP_")
            .AddCommandLine(overrides)
            .Build();
        using var configLifetime = config as IDisposable;
        var options = config.Get<AppOptions>() ?? new AppOptions();
        options.EnvironmentName = environment;
        var legacy = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
        if (!string.IsNullOrWhiteSpace(legacy)) options.Storage.DataPath = legacy;
        if (string.IsNullOrWhiteSpace(options.Orders.Currency))
            throw new ArgumentException("Orders:Currency не може бути порожнім.");
        if (options.Logging.MinimumLevel is not ("Debug" or "Information" or "Warning" or "Error" or "None"))
            throw new ArgumentException("Logging:MinimumLevel: Debug, Information, Warning, Error або None.");
        return new(options, options.Storage.ResolveFullPath(AppContext.BaseDirectory),
            !string.IsNullOrWhiteSpace(config["Integration:ApiKey"]));
    }

    public void Describe()
    {
        Console.WriteLine($"Профіль: {Options.EnvironmentName}");
        Console.WriteLine($"Файл даних : {DataFile}");
        Console.WriteLine(HasApiKey ? "ApiKey: присутній" : "ApiKey: не заданий");
        Console.WriteLine($"Валюта: {Options.Orders.Currency}");
        if (Options.Logging.MinimumLevel == "Debug")
            Console.WriteLine($"[Debug] Базовий каталог: {AppContext.BaseDirectory}");
    }
}
