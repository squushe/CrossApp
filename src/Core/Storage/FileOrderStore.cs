namespace Core.Storage;

using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

public sealed class FileOrderStore(string path) : IOrderStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;
        if (File.Exists(_path))
        {
            var dtos = JsonSerializer.Deserialize<List<OrderDto>>(File.ReadAllText(_path)) ?? [];
            foreach (var dto in dtos)
            {
                var p = Order.FromDto(dto);
                _cache[p.Id] = p;
            }
        }
        _loaded = true;
    }

    private void Flush()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(_cache.Values.Select(p => p.ToDto()).ToList(), Options));
    }

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
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");
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
