using Cli.Sinks;
using Core.Abstractions;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

IOrderStore store = StoreFactory.Create(args);
var service = new OrderService(store);

var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

void OnOrderChanged(object? sender, OrderChangedEventArgs e)
{
    var line = $"{e.At:HH:mm:ss} {e.Kind,-14} {e.OrderId} рядків: {e.LinesCount,2} → сума {e.Total,8:F2} грн";
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

service.Changed += OnOrderChanged;

int changes = 0;
service.Changed += (_, _) => changes++;

Console.WriteLine("=== Сценарій 1: Додавання та зміна замовлення з подіями ===");
var created = service.CreateOrder("CUST-101");
service.AddLine(created.Id, "P-101", "Кабель UTP cat6", 25.50m, 10);
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine("\n=== Сценарій 2: Відписка локального методу (-=) ===");
service.Changed -= OnOrderChanged;
service.AddLine(created.Id, "P-102", "Конектор RJ-45", 2.00m, 20);
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine("\n=== Сценарій 3: Відмова (порушення інваріанту) ===");
try
{
    service.AddLine(created.Id, "P-999", "Невалідний товар", -50.00m, 1);
}
catch (Exception ex)
{
    Console.WriteLine($"Помилка інваріанту: {ex.Message}");
}
Console.WriteLine($"Подій після помилки: {changes}");

Console.WriteLine("\n=== Сценарій 4: Делегат Func замість події (пошук за предикатом) ===");
var expensiveOrders = service.FindAll(o => o.Total > 200m);
Console.WriteLine($"Знайдено замовлень на суму > 200 грн: {expensiveOrders.Count}");
foreach (var o in expensiveOrders)
{
    Console.WriteLine($" - {o.Id} (Клієнт {o.CustomerId}): {o.Total:F2} грн ({o.Status})");
}

Console.WriteLine("\n=== Додаткове завдання 1: Ручний DI з INotificationSink (ConsoleSink) ===");
var consoleSinkService = new OrderService(store, new ConsoleSink());
var sinkOrder = consoleSinkService.CreateOrder("CUST-SINK");
consoleSinkService.AddLine(sinkOrder.Id, "P-103", "Патч-корд 1м", 45.00m, 2);

Console.WriteLine("\n=== Додаткове завдання 2: Action<string> для звіту про пропущені рядки ===");
string sampleCsvPath = Path.Combine(AppContext.BaseDirectory, "data", "sample.csv");
if (!File.Exists(sampleCsvPath))
{
    sampleCsvPath = "data/sample.csv";
}
if (File.Exists(sampleCsvPath))
{
    ProductCsvImporter.Load(sampleCsvPath, skipped => Console.WriteLine($" [Звіт пропущеного рядка]: {skipped}"));
}