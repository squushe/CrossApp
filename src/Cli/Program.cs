using Core.Domain;

Console.WriteLine("=== Сценарій 1: успіх ===");
Order order = Order.Create("O-100", "C-001");
order.AddLine("P-001", "Ноутбук Dell XPS 15", 1450.50m, 1);
order.AddLine("P-002", "Мишка Logitech MX Master 3", 99.99m, 2);
Console.WriteLine(order);

order.Confirm();
Console.WriteLine(order);

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

TryDo("додавання рядка до підтвердженого замовлення", () => order.AddLine("P-003", "Килимок", 15.50m, 1));
TryDo("підтвердження вже підтвердженого замовлення", () => order.Confirm());
TryDo("скасування підтвердженого замовлення", () => order.Cancel());
TryDo("порожній ID замовлення", () => Order.Create("   ", "C-002"));
TryDo("від'ємна ціна товару", () => OrderLine.Create("P-004", "Тест", -50.00m, 1));
TryDo("від'ємна кількість товару", () => OrderLine.Create("P-005", "Тест", 50.00m, -1));
TryDo("підтвердження порожнього замовлення", () => {
    var emptyOrder = Order.Create("O-101", "C-002");
    emptyOrder.Confirm();
});

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message}");
    }
}