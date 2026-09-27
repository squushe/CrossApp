using Core;
using Core.Domain;
using Core.Dto;
using Core.Services;

Console.WriteLine("=== Сценарій 1: успіх ===");
Order order = Order.Create("O-100", "C-001");
order.AddLine("P-001", "Ноутбук Dell XPS 15", 1450.50m, 1);
order.AddLine("P-002", "Мишка Logitech MX Master 3", 99.99m, 2);
Console.WriteLine(order);
order.Confirm();
Console.WriteLine(order);

Console.WriteLine("\n=== Сценарій 2: порушення інваріантів ===");
TryDo("додавання рядка до підтвердженого замовлення", () => order.AddLine("P-003", "Килимок", 15.50m, 1));
TryDo("скасування підтвердженого замовлення", () => order.Cancel());
TryDo("від'ємна ціна товару", () => OrderLine.Create("P-004", "Тест", -50.00m, 1));

Console.WriteLine("\n=== Сценарій 3: Додаткове завдання (Сервіс) ===");
var existingOrders = new List<Order>
{
    Order.Create("O-201", "C-999"),
    Order.Create("O-202", "C-999"),
    Order.Create("O-203", "C-999")
};
TryDo("перевищення ліміту відкритих замовлень", () => OrderService.CreateOrderForCustomer("C-999", existingOrders));

Console.WriteLine("\n=== Сценарій 4: Додаткове завдання (Import Mapper) ===");
var fakeDto = new OrderDto("O-300", "C-777", new List<OrderLineDto>
{
    new("P-001", "Поганий Товар", -10m, 1)
}, "Draft");

var importResult = new ImportResult<DomainRecord>(new List<DomainRecord> { fakeDto }, new List<string>());
var mappedResult = ImportMapper.MapOrders(importResult);

Console.WriteLine($"Успішно відновлено сутностей: {mappedResult.ValidOrders.Count}");
Console.WriteLine($"Помилок при відновленні: {mappedResult.Errors.Count}");
foreach (var error in mappedResult.Errors)
{
    Console.WriteLine($" ! {error}");
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" {title}: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message}");
    }
}