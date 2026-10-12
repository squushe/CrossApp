using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

IOrderStore store = useFile
    ? new FileOrderStore(dataPath)
    : new InMemoryOrderStore(SampleData.Orders());

var service = new OrderService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");

var created = service.CreateOrder("CUST-999");
service.AddLine(created.Id, "P-101", "Кабель UTP cat6", 25.50m, 10);
service.Confirm(created.Id);

Console.WriteLine("\nСписок усіх замовлень:");
foreach (var p in service.All())
{
    Console.WriteLine($" {p.Id,-10} {p.CustomerId,-10} {p.Status,-10} {p.Total,8:F2} грн ({p.Lines.Count} поз.)");
}

Console.WriteLine("\nПошук замовлення:");
var found = service.Find(created.Id);
if (found is not null)
{
    Console.WriteLine($" Знайдено: {found.Id}, Клієнт: {found.CustomerId}, Сума: {found.Total:F2} грн, Статус: {found.Status}");
}

Console.WriteLine("\nСценарії помилок:");
try
{
    service.AddLine("non-existent-id", "P-999", "Тестовий товар", 10.00m, 1);
}
catch (Exception ex)
{
    Console.WriteLine($" Помилка (неіснуючий id): {ex.Message}");
}

try
{
    store.Add(created);
}
catch (Exception ex)
{
    Console.WriteLine($" Помилка (дублікат id): {ex.Message}");
}