using Core.Abstractions;
using Core.Import;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public sealed class CancellationTests
{
    [Fact]
    public async Task ImportAsync_WithLinkedTokens_IsCancelledByTimeoutAndNotUser()
    {
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);
        var importer = new WaitingImporter();
        var service = new OrderService(new InMemoryOrderStore());
        var operation = service.ImportAsync(importer, "unused.json", linkedCts.Token);
        // Чекаємо входу в імпортер, а потім запускаємо справжній таймер.
        await importer.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(20));

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => operation.WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.True(timeoutCts.IsCancellationRequested);
        Assert.False(userCts.IsCancellationRequested);
        Assert.True(linkedCts.IsCancellationRequested);
        Assert.Equal(linkedCts.Token, error.CancellationToken);
        Assert.Equal(linkedCts.Token, importer.ReceivedToken);
    }

    private sealed class WaitingImporter : IOrderLineImporter
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken ReceivedToken { get; private set; }

        public async Task<IReadOnlyList<ImportedOrderLine>> LoadAsync(string path, CancellationToken ct = default)
        {
            ReceivedToken = ct;
            Started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return [];
        }
    }
}
