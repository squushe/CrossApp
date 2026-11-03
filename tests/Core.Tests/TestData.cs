using Core.Domain;

namespace Core.Tests;

internal static class TestData
{
    public static Order Order(string id = "ORD-A", decimal price = 10m, int quantity = 1)
    {
        var order = Core.Domain.Order.Create(id, "CUSTOMER-1");
        order.AddLine("P-BASE", "Базовий товар", price, quantity);
        return order;
    }

    public static IReadOnlyList<Order> Orders() =>
        [Order("ORD-C", 50m), Order("ORD-B", 200m), Order("ORD-A", 200m)];
}
