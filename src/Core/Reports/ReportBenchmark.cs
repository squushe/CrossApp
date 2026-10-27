using System.Diagnostics;
using Core.Domain;

namespace Core.Reports;

public static class ReportBenchmark
{
    public static BenchmarkResult Run()
    {
        var source = SampleData.GenerateOrders(100_000);
        const int iterations = 6;
        // Генерація, прогрів і перевірка результатів не входять у вимірювання.
        var expected = ReportService.ExpensiveOrders(source, 200m);
        var actual = WithIntermediateList(source);
        var equal = expected.SequenceEqual(actual);
        long withoutTicks = 0, withTicks = 0;
        for (int i = 0; i < iterations; i++)
        {
            // Чергуємо порядок, щоб один варіант не запускався завжди першим.
            if (i % 2 == 0)
            {
                withoutTicks += Measure(() => ReportService.ExpensiveOrders(source, 200m));
                withTicks += Measure(() => WithIntermediateList(source));
            }
            else
            {
                withTicks += Measure(() => WithIntermediateList(source));
                withoutTicks += Measure(() => ReportService.ExpensiveOrders(source, 200m));
            }
        }
        return new BenchmarkResult(source.Count, iterations, expected.Count,
            withoutTicks * 1000d / Stopwatch.Frequency / iterations,
            withTicks * 1000d / Stopwatch.Frequency / iterations, equal);
    }

    private static long Measure(Func<IReadOnlyList<OrderReportLine>> report)
    {
        var timer = Stopwatch.StartNew();
        var result = report();
        timer.Stop();
        GC.KeepAlive(result);
        return timer.ElapsedTicks;
    }

    private static IReadOnlyList<OrderReportLine> WithIntermediateList(IEnumerable<Order> source) =>
        source.Where(o => o.Total > 200m)
            .ToList()
            .OrderByDescending(o => o.Total)
            .ThenBy(o => o.Id, StringComparer.Ordinal)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Status.ToString(), o.Lines.Count, o.Total))
            .ToList();
}
