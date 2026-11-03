using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public sealed class ImportTests
{
    [Fact]
    public async Task ImportAsync_AddsRowsRaisesEventsAndSkipsDuplicates()
    {
        var order = TestData.Order();
        var service = new OrderService(new InMemoryOrderStore([order]));
        var importer = new StubImporter([
            new(order.Id, OrderLine.Create("P-NEW", "Новий товар", 7m, 3)),
            new(order.Id, OrderLine.Create("p-new", "Повтор", 7m, 3))]);
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);
        using var cts = new CancellationTokenSource();

        var added = await service.ImportAsync(importer, "unused.json", cts.Token);
        var repeated = await service.ImportAsync(importer, "unused.json", cts.Token);

        Assert.Equal(1, added);
        Assert.Equal(0, repeated);
        Assert.Equal(cts.Token, importer.ReceivedToken);
        Assert.Equal(31m, order.Total);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(OrderChangeKind.LineAdded, Assert.Single(events).Kind);
    }

    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_ThrowsBeforeOpeningFile()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonOrderLineImporter(), "any.json", cts.Token));

        Assert.Empty(store.List());
    }

    [Fact]
    public async Task LoadAsync_WhenAlreadyCancelled_ThrowsBeforeOpeningFile()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new JsonOrderLineImporter().LoadAsync("any.json", cts.Token));
    }

    [Fact]
    public async Task ImportAsync_WhenCancelledAfterFirstRow_StopsBeforeSecondRow()
    {
        using var cts = new CancellationTokenSource();
        var order = TestData.Order();
        var service = new OrderService(new InMemoryOrderStore([order]));
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => { events.Add(e); cts.Cancel(); };
        var importer = new StubImporter([
            new(order.Id, OrderLine.Create("P-FIRST", "Перший", 1m, 1)),
            new(order.Id, OrderLine.Create("P-SECOND", "Другий", 1m, 1))]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(importer, "unused.json", cts.Token));

        Assert.Single(events);
        Assert.Equal(11m, order.Total);
        Assert.DoesNotContain(order.Lines, line => line.ProductId == "P-SECOND");
    }

    [Fact]
    public async Task ImportAsync_ToConfirmedOrder_RejectsRowWithoutEvent()
    {
        var order = TestData.Order();
        order.Confirm();
        var service = new OrderService(new InMemoryOrderStore([order]));
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);
        var importer = new StubImporter([
            new(order.Id, OrderLine.Create("P-NEW", "Новий товар", 1m, 1))]);

        var error = await Assert.ThrowsAsync<DomainException>(
            () => service.ImportAsync(importer, "unused.json"));

        Assert.Contains("Запис 1", error.Message);
        Assert.Single(order.Lines);
        Assert.Empty(events);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidRecord_ReportsNumberAndPreservesInnerException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, """
                [
                  { "OrderId": "ORD-A", "ProductId": "P-1", "Name": "Valid", "Price": 10, "Quantity": 1 },
                  { "OrderId": "ORD-A", "ProductId": "P-2", "Name": "Invalid", "Price": 10, "Quantity": -1 }
                ]
                """);
            var order = TestData.Order();
            var service = new OrderService(new InMemoryOrderStore([order]));

            var error = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonOrderLineImporter(), path));

            Assert.Contains("Запис 2", error.Message);
            Assert.IsType<ArgumentOutOfRangeException>(error.InnerException);
            Assert.Single(order.Lines);
            Assert.Equal(10m, order.Total);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class StubImporter(IReadOnlyList<ImportedOrderLine> items) : IOrderLineImporter
    {
        public CancellationToken ReceivedToken { get; private set; }

        public Task<IReadOnlyList<ImportedOrderLine>> LoadAsync(string path, CancellationToken ct = default)
        {
            ReceivedToken = ct;
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(items);
        }
    }
}
