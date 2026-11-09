namespace Core.Storage;

using Core.Abstractions;
using Core.Domain;

public sealed class FileOrderStore(string path) : IOrderStore
{
    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;
        if (File.Exists(_path))
        {
            // Повна перевірка відбувається до зміни кешу.
            var orders = OrderJsonFile.Read(_path);
            foreach (var order in orders) _cache.Add(order.Id, order);
        }
        _loaded = true;
    }

    private void Flush() => OrderJsonFile.Write(_path, _cache.Values);

    public IReadOnlyList<Order> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Order? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate)
    {
        EnsureLoaded();
        return _cache.Values.Where(predicate).ToList();
    }

    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (_cache.ContainsKey(item.Id))
            throw new DomainException($"Запис з id={item.Id} уже існує.");
        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(Order item)
    {
        EnsureLoaded();
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }
}
