using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    // Звіти охоплюють усі статуси; Total означає вартість замовлень, а не отриману виручку.
    public static IReadOnlyList<CustomerSummary> TopCustomers(IEnumerable<Order> source, int n) =>
        source.GroupBy(o => o.CustomerId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CustomerSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(g => g.Total)
            .ThenBy(g => g.CustomerId, StringComparer.Ordinal)
            .Take(n)
            .ToList();

    public static IReadOnlyList<OrderReportLine> ExpensiveOrders(IEnumerable<Order> source, decimal threshold) =>
        source.Where(o => o.Total > threshold)
            .OrderByDescending(o => o.Total)
            .ThenBy(o => o.Id, StringComparer.Ordinal)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Status.ToString(), o.Lines.Count, o.Total))
            .ToList();

    public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status)
            .Select(g => new GroupSummary(g.Key.ToString(), g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(g => g.Total)
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

    public static (int Items, decimal Total, decimal Average, decimal Max) Totals(IEnumerable<Order> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0m, 0m, 0m)
            : (list.Count, list.Sum(o => o.Total), list.Average(o => o.Total), list.Max(o => o.Total));
    }

    public static DeferredExecutionResult DemonstrateDeferredExecution(IEnumerable<Order> orders)
    {
        // Змінюємо лише локальний список: сховище та доменні об'єкти не змінюються.
        var source = orders.ToList();
        var query = source.Where(o => o.Total > 200m);
        var before = query.Count();
        var temporary = Order.Create("DEMO-ONLY", "CUST-DEMO");
        temporary.AddLine("P-DEMO", "Демонстраційний товар", 201m, 1);
        source.Add(temporary);
        var after = query.Count();
        var snapshot = query.ToList();
        source.Clear();
        return new DeferredExecutionResult(before, after, snapshot.Count, query.Count());
    }
}
