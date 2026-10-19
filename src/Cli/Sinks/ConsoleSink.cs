namespace Cli.Sinks;

using Core.Abstractions;
using Core.Events;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(OrderChangedEventArgs args)
    {
        Console.WriteLine($"[ConsoleSink] {args.At:HH:mm:ss} {args.Kind} {args.OrderId} -> {args.Total:F2} грн");
    }
}
