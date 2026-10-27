namespace Core;

using Core.Domain;

public static class SampleData
{
    public static IReadOnlyList<Order> Orders()
    {
        var orders = new List<Order>();

        var o1 = Order.Create("ORD-001", "CUST-001");
        o1.AddLine("P-001", "Ноутбук Dell XPS 15", 1450.50m, 1);
        o1.Confirm();
        orders.Add(o1);

        var o2 = Order.Create("ORD-002", "CUST-002");
        o2.AddLine("P-002", "Мишка Logitech MX Master 3", 99.99m, 2);
        o2.AddLine("P-006", "Килимок для миші", 15.50m, 2);
        o2.Confirm();
        orders.Add(o2);

        var o3 = Order.Create("ORD-003", "CUST-001");
        o3.AddLine("P-003", "Клавіатура Keychron K2", 85.00m, 1);
        orders.Add(o3);

        var o4 = Order.Create("ORD-004", "CUST-003");
        o4.AddLine("P-004", "Монітор LG UltraGear", 350.25m, 1);
        o4.AddLine("P-008", "USB-C Хаб Baseus", 35.90m, 1);
        o4.Confirm();
        orders.Add(o4);

        var o5 = Order.Create("ORD-005", "CUST-004");
        o5.AddLine("P-005", "Навушники Sony WH-1000XM4", 298.00m, 1);
        o5.Cancel();
        orders.Add(o5);

        var o6 = Order.Create("ORD-006", "CUST-002");
        o6.AddLine("P-007", "Вебкамера Logitech C920", 65.00m, 1);
        o6.Confirm();
        orders.Add(o6);

        var o7 = Order.Create("ORD-007", "CUST-005");
        o7.AddLine("P-009", "Мікрофон Blue Yeti", 120.00m, 1);
        o7.AddLine("P-006", "Килимок для миші", 15.50m, 1);
        o7.Confirm();
        orders.Add(o7);

        var o8 = Order.Create("ORD-008", "CUST-006");
        o8.AddLine("P-010", "Крісло Cougar Armor", 250.00m, 1);
        orders.Add(o8);

        var o9 = Order.Create("ORD-009", "CUST-003");
        o9.AddLine("P-001", "Ноутбук Dell XPS 15", 1450.50m, 2);
        o9.AddLine("P-004", "Монітор LG UltraGear", 350.25m, 2);
        o9.Confirm();
        orders.Add(o9);

        var o10 = Order.Create("ORD-010", "CUST-007");
        o10.AddLine("P-003", "Клавіатура Keychron K2", 85.00m, 3);
        o10.Confirm();
        orders.Add(o10);

        var o11 = Order.Create("ORD-011", "CUST-008");
        o11.AddLine("P-002", "Мишка Logitech MX Master 3", 99.99m, 1);
        orders.Add(o11);

        var o12 = Order.Create("ORD-012", "CUST-005");
        o12.AddLine("P-008", "USB-C Хаб Baseus", 35.90m, 4);
        o12.Confirm();
        orders.Add(o12);

        var o13 = Order.Create("ORD-013", "CUST-009");
        o13.AddLine("P-005", "Навушники Sony WH-1000XM4", 298.00m, 2);
        o13.Cancel();
        orders.Add(o13);

        var o14 = Order.Create("ORD-014", "CUST-010");
        o14.AddLine("P-007", "Вебкамера Logitech C920", 65.00m, 2);
        o14.AddLine("P-009", "Мікрофон Blue Yeti", 120.00m, 1);
        o14.Confirm();
        orders.Add(o14);

        var o15 = Order.Create("ORD-015", "CUST-004");
        o15.AddLine("P-010", "Крісло Cougar Armor", 250.00m, 1);
        o15.AddLine("P-006", "Килимок для миші", 15.50m, 1);
        o15.Confirm();
        orders.Add(o15);

        var o16 = Order.Create("ORD-016", "CUST-006");
        o16.AddLine("P-002", "Мишка Logitech MX Master 3", 99.99m, 1);
        o16.AddLine("P-003", "Клавіатура Keychron K2", 85.00m, 1);
        orders.Add(o16);

        var o17 = Order.Create("ORD-017", "CUST-011");
        o17.AddLine("P-011", "Кабель USB", 25m, 2);
        orders.Add(o17);

        var o18 = Order.Create("ORD-018", "CUST-012");
        o18.AddLine("P-012", "Адаптер USB", 50m, 1);
        o18.Confirm();
        orders.Add(o18);

        return orders;
    }
}
