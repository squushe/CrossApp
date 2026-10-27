namespace Core.Reports;

public sealed record CustomerSummary(string CustomerId, int OrdersCount, decimal Total);
