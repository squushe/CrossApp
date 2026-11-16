using Core.Domain;
using Core.Storage;

namespace Core.Tests;

public sealed class AsyncOrderStoreTests
{
    [Fact]
    public async Task Store_PreservesTimestampAndIsolatesMutableOrders()
    {
        var store = new AsyncInMemoryOrderStore();
        var order = TestData.Order();
        await store.AddAsync(order);
        order.AddLine("EXTERNAL", "Зовнішня зміна", 10, 1);
        var saved = (await store.GetByIdAsync(order.Id))!;
        Assert.Single(saved.Lines);
        Assert.Equal(order.CreatedAt, saved.CreatedAt);
        saved.Cancel();
        Assert.Equal(OrderStatus.Draft, (await store.GetByIdAsync(order.Id))!.Status);
        Assert.True(await store.RemoveAsync(order.Id));
        Assert.False(await store.UpdateAsync(saved)); // PUT не відновлює видалений запис.
        Assert.Null(await store.GetByIdAsync(order.Id));
    }

    [Fact]
    public async Task ParallelAdds_DoNotLoseOrders()
    {
        var store = new AsyncInMemoryOrderStore();
        await Task.WhenAll(Enumerable.Range(0, 100).Select(i =>
            Task.Run(() => store.AddAsync(TestData.Order($"O-{i}")))));
        Assert.Equal(100, (await store.ListAsync()).Count);
    }

    [Fact]
    public async Task CancelledWrite_DoesNotModifyStore()
    {
        var store = new AsyncInMemoryOrderStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await store.AddAsync(TestData.Order(), cts.Token));
        Assert.Empty(await store.ListAsync());
    }
}
