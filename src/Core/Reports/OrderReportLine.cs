namespace Core.Reports;

public sealed record OrderReportLine(string Id, string CustomerId, string Status, int LinesCount, decimal Total);
