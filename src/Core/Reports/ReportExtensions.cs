namespace Core.Reports;

public static class ReportExtensions
{
    public static IEnumerable<T> TopBy<T, TKey>(this IEnumerable<T> source,
        Func<T, TKey> score, int n, Func<T, string> id) =>
        source.OrderByDescending(score)
            .ThenBy(id, StringComparer.Ordinal)
            .Take(n);
}
