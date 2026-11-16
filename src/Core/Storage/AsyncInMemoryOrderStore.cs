using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Singleton: кожна операція атомарна; назовні повертаємо незалежні копії.
public sealed class AsyncInMemoryOrderStore : IAsyncOrderStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Order> _items = new(StringComparer.OrdinalIgnoreCase);
    private static Order Copy(Order order) => Order.FromDto(order.ToDto());

    public Task<IReadOnlyList<Order>> ListAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Order>>(_items.Values.Select(Copy).ToList());
        }
    }

    public Task<Order?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        lock (_gate)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(_items.TryGetValue(id, out var order) ? Copy(order) : null);
        }
    }

    public Task AddAsync(Order order, CancellationToken ct = default)
    {
        lock (_gate)
        {
            ct.ThrowIfCancellationRequested();
            if (!_items.TryAdd(order.Id, Copy(order)))
                throw new DomainException($"Запис з id={order.Id} уже існує.");
            return Task.CompletedTask;
        }
    }

    public Task<bool> UpdateAsync(Order order, CancellationToken ct = default)
    {
        lock (_gate)
        {
            ct.ThrowIfCancellationRequested();
            if (!_items.ContainsKey(order.Id)) return Task.FromResult(false);
            _items[order.Id] = Copy(order);
            return Task.FromResult(true);
        }
    }

    public Task<bool> RemoveAsync(string id, CancellationToken ct = default)
    {
        lock (_gate)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(_items.Remove(id));
        }
    }
}
