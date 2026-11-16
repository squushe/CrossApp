using Core.Domain;

namespace Core.Abstractions;

// HTTP та майбутнє EF-сховище мають один асинхронний контракт.
public interface IAsyncOrderStore
{
    Task<IReadOnlyList<Order>> ListAsync(CancellationToken ct = default);
    Task<Order?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<bool> UpdateAsync(Order order, CancellationToken ct = default);
    Task<bool> RemoveAsync(string id, CancellationToken ct = default);
}
