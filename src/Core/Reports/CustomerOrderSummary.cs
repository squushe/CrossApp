namespace Core.Reports;

public sealed record CustomerOrderSummary(string CustomerId, string Name, int OrdersCount, decimal Total);
