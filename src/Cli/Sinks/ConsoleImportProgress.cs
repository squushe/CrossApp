namespace Cli.Sinks;

// Синхронний IProgress: останнє повідомлення не губиться при завершенні Cli.
public sealed class ConsoleImportProgress : IProgress<int>
{
    private int _last = -1;

    public void Report(int value)
    {
        if (value <= _last) return;
        _last = value;
        Console.WriteLine($"Прогрес імпорту: {value}%");
    }
}
