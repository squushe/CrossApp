using Core.Reports;

namespace Core.Tests;

public sealed class ReportServiceTests
{
    [Theory]
    [InlineData(0, 3)]
    [InlineData(50, 2)]
    [InlineData(200, 0)]
    public void ExpensiveOrders_CountsOnlyAboveThreshold(int threshold, int expected)
    {
        var orders = TestData.Orders();

        var result = ReportService.ExpensiveOrders(orders, threshold);

        Assert.Equal(expected, result.Count);
    }

    [Fact]
    public void TopOrders_WithEqualTotals_SortsByIdRegardlessOfInputOrder()
    {
        var orders = TestData.Orders();

        var result = ReportService.TopOrders(orders, 2);
        var reversed = ReportService.TopOrders(orders.Reverse(), 2);

        Assert.Equal(new[] { "ORD-A", "ORD-B" }, result.Select(row => row.Id));
        Assert.Equal(result, reversed);
    }

    [Fact]
    public void Totals_WithEmptySource_ReturnsZeros()
    {
        var result = ReportService.Totals([]);

        Assert.Equal((0, 0m, 0m, 0m), result);
    }
}
