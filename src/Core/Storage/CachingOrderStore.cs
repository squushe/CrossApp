namespace Core.Storage;

using Core.Abstractions;
using Core.Domain;

public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _allLoaded;

    public IReadOnlyList<Order> List()
    {
        if (!_allLoaded)
        {
            var items = _inner.List();
            foreach (var item in items)
            {
                _cache[item.Id] = item;
            }
            _allLoaded = true;
        }
        return _cache.Values.ToList();
    }

    public Order? GetById(string id)
    {
        if (_cache.TryGetValue(id, out var cached))
            return cached;

        var item = _inner.GetById(id);
        if (item is not null)
            _cache[item.Id] = item;

        return item;
    }

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate) => List().Where(predicate).ToList();

    public void Add(Order item)
    {
        _inner.Add(item);
        _cache[item.Id] = item;
    }

    public void Update(Order item)
    {
        _inner.Update(item);
        _cache[item.Id] = item;
    }

    public bool Remove(string id)
    {
        var removed = _inner.Remove(id);
        if (removed)
            _cache.Remove(id);
        return removed;
    }
}
