using Core;
using Core.Reports;
using Core.Services;
using Core.Storage;

var service = new OrderService(StoreFactory.Create(args));
var items = service.All();
var command = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? "report-top";

switch (command)
{
    case "report-top":
        Console.WriteLine($"  {"Клієнт",-12} {"Замовлень",10} {"Сума, грн",14}");
        foreach (var row in ReportService.TopCustomers(items, 5))
            Console.WriteLine($"  {row.CustomerId,-12} {row.OrdersCount,10} {row.Total,14:F2}");
        break;
    case "report-customers":
        Console.WriteLine($"  {"Клієнт",-12} {"Ім'я",-14} {"Замовлень",10} {"Сума, грн",14}");
        foreach (var row in ReportService.ByCustomer(SampleData.Customers(), items))
            Console.WriteLine($"  {row.CustomerId,-12} {row.Name,-14} {row.OrdersCount,10} {row.Total,14:F2}");
        break;
    case "report-top-orders":
        Console.WriteLine($"  {"Замовлення",-12} {"Клієнт",-12} {"Сума, грн",14}");
        foreach (var row in ReportService.TopOrders(items, 5))
            Console.WriteLine($"  {row.Id,-12} {row.CustomerId,-12} {row.Total,14:F2}");
        break;
    case "report-expensive":
        Console.WriteLine("Замовлення дорожче 200 грн:");
        Console.WriteLine($"  {"Замовлення",-12} {"Клієнт",-12} {"Статус",-12} {"Рядків",7} {"Сума, грн",14}");
        foreach (var row in ReportService.ExpensiveOrders(items, 200m))
            Console.WriteLine($"  {row.Id,-12} {row.CustomerId,-12} {row.Status,-12} {row.LinesCount,7} {row.Total,14:F2}");
        break;
    case "report-groups":
        Console.WriteLine($"  {"Статус",-12} {"Замовлень",10} {"Сума, грн",14}");
        foreach (var row in ReportService.ByStatus(items))
            Console.WriteLine($"  {row.Key,-12} {row.Count,10} {row.Total,14:F2}");
        break;
    case "report-groups-query":
        Console.WriteLine($"  {"Статус",-12} {"Замовлень",10} {"Сума, грн",14}");
        foreach (var row in ReportService.ByStatusQuery(items))
            Console.WriteLine($"  {row.Key,-12} {row.Count,10} {row.Total,14:F2}");
        break;
    case "benchmark-reports":
        var benchmark = ReportBenchmark.Run();
        Console.WriteLine($"Записів: {benchmark.Records}, повторів: {benchmark.Iterations}, рядків звіту: {benchmark.ResultCount}");
        Console.WriteLine($"Без проміжного ToList: {benchmark.WithoutIntermediateListMs:F3} мс (середнє)");
        Console.WriteLine($"З проміжним ToList:   {benchmark.WithIntermediateListMs:F3} мс (середнє)");
        Console.WriteLine($"Результати однакові: {benchmark.ResultsEqual}");
        break;
    case "report-sum":
        var t = ReportService.Totals(items);
        Console.WriteLine($"Замовлень: {t.Items}, сума: {t.Total:F2} грн, середній чек: {t.Average:F2} грн, максимум: {t.Max:F2} грн");
        break;
    case "demo-deferred":
        var demo = ReportService.DemonstrateDeferredExecution(items);
        Console.WriteLine($"Дорожчих за 200 грн до змін: {demo.Before}");
        Console.WriteLine($"Дорожчих за 200 грн після додавання: {demo.After}");
        Console.WriteLine($"У знімку після очищення джерела: {demo.SnapshotCount}");
        Console.WriteLine($"У запиті після очищення джерела: {demo.AfterClear}");
        break;
    default:
        Console.WriteLine("report-top | report-expensive | report-groups | report-sum | demo-deferred | report-customers | report-top-orders | report-groups-query | benchmark-reports");
        Environment.ExitCode = 1;
        break;
}
