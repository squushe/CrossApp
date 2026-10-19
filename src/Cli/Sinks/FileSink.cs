namespace Cli.Sinks;

using Core.Abstractions;
using Core.Events;

public sealed class FileSink(string path) : INotificationSink
{
    private readonly string _path = Path.GetFullPath(path);

    public void Notify(OrderChangedEventArgs args)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var line = $"[FileSink] {args.At:HH:mm:ss} {args.Kind} {args.OrderId} -> {args.Total:F2} грн";
        File.AppendAllText(_path, line + Environment.NewLine);
    }
}
