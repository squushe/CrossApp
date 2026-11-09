namespace Core.Configuration;

public sealed class AppOptions
{
    public StorageOptions Storage { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public OrderOptions Orders { get; init; } = new();
    public string EnvironmentName { get; set; } = "Development";
}

public sealed class StorageOptions
{
    public string DataPath { get; set; } = "data";
    public string FileName { get; init; } = "orders.json";

    public string ResolveFullPath(string baseDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(DataPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(FileName);
        if (Path.GetFileName(FileName) != FileName || FileName is "." or "..")
            throw new ArgumentException("Storage:FileName має бути назвою файлу без каталогів.");
        var root = Path.IsPathRooted(DataPath) ? DataPath : Path.Combine(baseDir, DataPath);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; init; } = "Information";
}

public sealed class OrderOptions
{
    public string Currency { get; init; } = "UAH";
}
