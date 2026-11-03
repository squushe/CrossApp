using System.Runtime.CompilerServices;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public sealed class StreamingImportTests
{
    [Fact]
    public async Task ReadAsync_WithEnumeratorCancellation_ThrowsBeforeOpeningFile()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var importer = new JsonOrderLineImporter();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            // Токен передається перебиранню, а не параметром ReadAsync.
            await foreach (var _ in importer.ReadAsync("any.json").WithCancellation(cts.Token)) { }
        });
    }

    [Fact]
    public async Task ImportStreamingAsync_AppliesEachRowBeforeRequestingNextAndSkipsDuplicates()
    {
        var order = TestData.Order();
        var service = new OrderService(new InMemoryOrderStore([order]));
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);
        var importer = new SequenceImporter([
            new(order.Id, OrderLine.Create("P-NEW", "New", 7m, 3)),
            new(order.Id, OrderLine.Create("p-new", "Duplicate", 7m, 3))]);
        importer.BeforeSecond = () => Assert.Equal(31m, order.Total);
        using var cts = new CancellationTokenSource();
        var progress = new RecordingProgress();

        var added = await service.ImportStreamingAsync(importer, "unused.json", cts.Token, progress);
        var repeated = await service.ImportStreamingAsync(importer, "unused.json", cts.Token);

        Assert.Equal(1, added);
        Assert.Equal(0, repeated);
        Assert.Equal(cts.Token, importer.ReceivedToken);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(OrderChangeKind.LineAdded, Assert.Single(events).Kind);
        Assert.Equal(new[] { 0, 100 }, progress.Values);
    }

    [Fact]
    public async Task ImportStreamingAsync_WhenAlreadyCancelled_DoesNotOpenFile()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new OrderService(new InMemoryOrderStore());
        var progress = new RecordingProgress();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ImportStreamingAsync(
            new JsonOrderLineImporter(), "any.json", cts.Token, progress));

        Assert.Empty(progress.Values);
    }

    [Fact]
    public async Task ImportStreamingAsync_WhenCancelledAfterFirstRow_DoesNotReportCompletion()
    {
        using var cts = new CancellationTokenSource();
        var order = TestData.Order();
        var service = new OrderService(new InMemoryOrderStore([order]));
        service.Changed += (_, _) => cts.Cancel();
        var progress = new RecordingProgress();
        var importer = new SequenceImporter([
            new(order.Id, OrderLine.Create("P-FIRST", "First", 1m, 1)),
            new(order.Id, OrderLine.Create("P-SECOND", "Second", 1m, 1))]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ImportStreamingAsync(
            importer, "unused.json", cts.Token, progress));

        Assert.Equal(11m, order.Total);
        Assert.DoesNotContain(order.Lines, line => line.ProductId == "P-SECOND");
        Assert.DoesNotContain(100, progress.Values);
    }

    [Fact]
    public async Task ImportAsync_ReportsProcessedRowsIncludingSkippedDuplicates()
    {
        var order = TestData.Order();
        var service = new OrderService(new InMemoryOrderStore([order]));
        var importer = new SequenceImporter([
            new(order.Id, OrderLine.Create("P-BASE", "Duplicate", 10m, 1)),
            new(order.Id, OrderLine.Create("P-NEW", "New", 5m, 2))]);
        var progress = new RecordingProgress();

        var added = await service.ImportAsync(importer, "unused.json", progress: progress);

        Assert.Equal(1, added);
        Assert.Equal(new[] { 0, 50, 100 }, progress.Values);
    }

    [Fact]
    public async Task ImportStreamingAsync_WithInvalidSecondRecord_KeepsFirstAndDisposesStream()
    {
        var path = Path.Combine(Path.GetTempPath(), $"crossapp-stream-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, """
                [
                  { "OrderId": "ORD-A", "ProductId": "P-NEW", "Name": "Valid", "Price": 2, "Quantity": 3 },
                  { "OrderId": "ORD-A", "ProductId": "P-BAD", "Name": "Invalid", "Price": 2, "Quantity": -1 }
                ]
                """);
            var order = TestData.Order();
            var service = new OrderService(new InMemoryOrderStore([order]));
            var events = new List<OrderChangedEventArgs>();
            service.Changed += (_, e) => events.Add(e);
            var progress = new RecordingProgress();

            var error = await Assert.ThrowsAsync<DomainException>(() => service.ImportStreamingAsync(
                new JsonOrderLineImporter(), path, progress: progress));

            Assert.Contains("Запис 2", error.Message);
            Assert.IsType<ArgumentOutOfRangeException>(error.InnerException);
            Assert.Equal(16m, order.Total);
            Assert.Single(events);
            Assert.DoesNotContain(100, progress.Values);
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.Length > 0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImportAsync_WithEmptySource_ReportsCompletion(bool streaming)
    {
        var service = new OrderService(new InMemoryOrderStore());
        var importer = new SequenceImporter([]);
        var progress = new RecordingProgress();

        var added = streaming
            ? await service.ImportStreamingAsync(importer, "unused.json", progress: progress)
            : await service.ImportAsync(importer, "unused.json", progress: progress);

        Assert.Equal(0, added);
        Assert.Equal(new[] { 0, 100 }, progress.Values);
    }

    private sealed class RecordingProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];
        public void Report(int value) => Values.Add(value);
    }

    private sealed class SequenceImporter(IReadOnlyList<ImportedOrderLine> items)
        : IStreamingOrderLineImporter, IOrderLineImporter
    {
        public Action? BeforeSecond { get; set; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<IReadOnlyList<ImportedOrderLine>> LoadAsync(string path, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(items);
        }

        public async IAsyncEnumerable<ImportedOrderLine> ReadAsync(string path,
            IProgress<int>? progress = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ReceivedToken = ct;
            for (var i = 0; i < items.Count; i++)
            {
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
                if (i == 1) BeforeSecond?.Invoke();
                yield return items[i];
            }
        }
    }
}
