using System.Text.Json;
using Cli.Sinks;
using Cli.Configuration;
using Core;
using Core.Import;
using Core.Reports;
using Core.Services;
using Core.Storage;

try
{
    await RunAsync(args);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Імпорт скасовано.");
    Environment.ExitCode = 2;
}
catch (DomainException ex)
{
    Console.WriteLine($"Дані відхилено: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Файл не знайдено: {ex.FileName}");
    Environment.ExitCode = 1;
}
catch (JsonException ex)
{
    Console.WriteLine($"Некоректний JSON: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (InvalidDataException ex)
{
    Console.WriteLine($"Некоректна конфігурація: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (IOException ex)
{
    Console.WriteLine($"Помилка читання/запису файлу: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"Немає доступу до файлу: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Некоректні аргументи: {ex.Message}");
    Environment.ExitCode = 1;
}

static async Task RunAsync(string[] args)
{
    var settings = CliConfiguration.Load(args);
    settings.Describe();
    var sumHeader = $"Сума, {settings.Options.Orders.Currency}";
    var service = new OrderService(StoreFactory.Create(args, settings.DataFile));
    var items = service.All();
    var command = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? "report-top";

    switch (command)
    {
        case "config":
            break;
        case "export":
            OrderJsonFile.Write(settings.DataFile, items);
            Console.WriteLine($"Експортовано {items.Count} замовлень.");
            break;
        case "import":
            var index = Array.IndexOf(args, "import");
            var source = index + 1 < args.Length && !args[index + 1].StartsWith("--")
                ? Path.GetFullPath(args[index + 1], AppContext.BaseDirectory)
                : settings.DataFile;
            var imported = OrderJsonFile.Read(source);
            OrderJsonFile.Write(settings.DataFile, imported);
            Console.WriteLine($"Імпортовано {imported.Count} замовлень. Колекцію замінено повністю.");
            break;
        case "import-lines":
            await ImportLinesAsync(service, args);
            break;
        case "report-top":
            Console.WriteLine($"  {"Клієнт",-12} {"Замовлень",10} {sumHeader,14}");
            foreach (var row in ReportService.TopCustomers(items, 5))
                Console.WriteLine($"  {row.CustomerId,-12} {row.OrdersCount,10} {row.Total,14:F2}");
            break;
        case "report-customers":
            Console.WriteLine($"  {"Клієнт",-12} {"Ім'я",-14} {"Замовлень",10} {sumHeader,14}");
            foreach (var row in ReportService.ByCustomer(SampleData.Customers(), items))
                Console.WriteLine($"  {row.CustomerId,-12} {row.Name,-14} {row.OrdersCount,10} {row.Total,14:F2}");
            break;
        case "report-top-orders":
            Console.WriteLine($"  {"Замовлення",-12} {"Клієнт",-12} {sumHeader,14}");
            foreach (var row in ReportService.TopOrders(items, 5))
                Console.WriteLine($"  {row.Id,-12} {row.CustomerId,-12} {row.Total,14:F2}");
            break;
        case "report-expensive":
            Console.WriteLine($"Замовлення дорожче 200 {settings.Options.Orders.Currency}:");
            Console.WriteLine($"  {"Замовлення",-12} {"Клієнт",-12} {"Статус",-12} {"Рядків",7} {sumHeader,14}");
            foreach (var row in ReportService.ExpensiveOrders(items, 200m))
                Console.WriteLine($"  {row.Id,-12} {row.CustomerId,-12} {row.Status,-12} {row.LinesCount,7} {row.Total,14:F2}");
            break;
        case "report-groups":
            Console.WriteLine($"  {"Статус",-12} {"Замовлень",10} {sumHeader,14}");
            foreach (var row in ReportService.ByStatus(items))
                Console.WriteLine($"  {row.Key,-12} {row.Count,10} {row.Total,14:F2}");
            break;
        case "report-groups-query":
            Console.WriteLine($"  {"Статус",-12} {"Замовлень",10} {sumHeader,14}");
            foreach (var row in ReportService.ByStatusQuery(items))
                Console.WriteLine($"  {row.Key,-12} {row.Count,10} {row.Total,14:F2}");
            break;
        case "benchmark-reports":
            var benchmark = ReportBenchmark.Run();
            Console.WriteLine($"Записів: {benchmark.Records}, повторів: {benchmark.Iterations}, рядків звіту: {benchmark.ResultCount}");
            Console.WriteLine($"Без проміжного ToList: {benchmark.WithoutIntermediateListMs:F3} мс (середнє)");
            Console.WriteLine($"З проміжним ToList:   {benchmark.WithIntermediateListMs:F3} мс (середнє)");
            Console.WriteLine($"Результати однакові: {benchmark.ResultsEqual}");
            break;
        case "report-sum":
            var t = ReportService.Totals(items);
            Console.WriteLine($"Замовлень: {t.Items}, сума: {t.Total:F2} {settings.Options.Orders.Currency}, середній чек: {t.Average:F2} {settings.Options.Orders.Currency}, максимум: {t.Max:F2} {settings.Options.Orders.Currency}");
            break;
        case "demo-deferred":
            var demo = ReportService.DemonstrateDeferredExecution(items);
            Console.WriteLine($"Дорожчих за 200 грн до змін: {demo.Before}");
            Console.WriteLine($"Дорожчих за 200 грн після додавання: {demo.After}");
            Console.WriteLine($"У знімку після очищення джерела: {demo.SnapshotCount}");
            Console.WriteLine($"У запиті після очищення джерела: {demo.AfterClear}");
            break;
        default:
            Console.WriteLine("config | export [--file] | import [path] | import-lines [path] [--stream] [--progress] [--cancel-after-ms N] [--file] | report-top | report-expensive | report-groups | report-sum | demo-deferred | report-customers | report-top-orders | report-groups-query | benchmark-reports");
            Environment.ExitCode = 1;
            break;
    }
}

static async Task ImportLinesAsync(OrderService service, string[] args)
{
    // Шлях стоїть відразу після команди; параметри - після шляху.
    var commandIndex = Array.IndexOf(args, "import-lines");
    var path = commandIndex + 1 < args.Length && !args[commandIndex + 1].StartsWith("--")
        ? args[commandIndex + 1]
        : Path.Combine(AppContext.BaseDirectory, "samples", "order-lines.json");
    path = Path.GetFullPath(path);
    using var userCts = new CancellationTokenSource();
    using var timeoutCts = new CancellationTokenSource();
    var timeoutIndex = Array.IndexOf(args, "--cancel-after-ms");
    if (timeoutIndex >= 0)
    {
        if (timeoutIndex + 1 >= args.Length ||
            !int.TryParse(args[timeoutIndex + 1], out var milliseconds) || milliseconds < 0)
            throw new ArgumentException("--cancel-after-ms потребує невід'ємного цілого числа.");
        if (milliseconds == 0)
            timeoutCts.Cancel(); // Детермінована демонстрація скасування навіть на малому файлі.
        else
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(milliseconds));
    }

    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);
    IProgress<int>? progress = args.Contains("--progress") ? new ConsoleImportProgress() : null;
    ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; userCts.Cancel(); };
    Console.CancelKeyPress += handler;
    try
    {
        var importer = new JsonOrderLineImporter();
        var added = args.Contains("--stream")
            ? await service.ImportStreamingAsync(importer, path, linkedCts.Token, progress)
            : await service.ImportAsync(importer, path, linkedCts.Token, progress);
        Console.WriteLine($"Імпортовано рядків: {added}");
    }
    catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
    {
        Console.WriteLine(userCts.IsCancellationRequested
            ? "Джерело скасування: Ctrl+C." : "Джерело скасування: таймаут.");
        throw;
    }
    finally
    {
        Console.CancelKeyPress -= handler;
    }
}
