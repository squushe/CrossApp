namespace Core.Services;

using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;

public sealed class OrderService(IOrderStore store, INotificationSink? sink = null)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink? _sink = sink;

    public event EventHandler<OrderChangedEventArgs>? Changed;

    private void OnChanged(Order order, OrderChangeKind kind)
    {
        var args = new OrderChangedEventArgs(order.Id, kind, order.Lines.Count, order.Total);
        Changed?.Invoke(this, args);
        _sink?.Notify(args);
    }

    public Order CreateOrder(string customerId)
    {
        var order = Order.Create(Guid.NewGuid().ToString("N")[..8], customerId);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.OrderCreated);
        return order;
    }

    public void AddLine(string id, string productId, string name, decimal price, int quantity)
    {
        var order = _store.GetById(id)
            ?? throw new DomainException($"Немає запису з id={id}.");
        order.AddLine(productId, name, price, quantity);
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded);
    }

    public void Confirm(string id)
    {
        var order = _store.GetById(id)
            ?? throw new DomainException($"Немає запису з id={id}.");
        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderConfirmed);
    }

    public void Cancel(string id)
    {
        var order = _store.GetById(id)
            ?? throw new DomainException($"Немає запису з id={id}.");
        order.Cancel();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderCancelled);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public async Task<int> ImportAsync(IOrderLineImporter importer, string path,
        CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();
        progress?.Report(0);
        var items = await importer.LoadAsync(path, ct);
        var added = 0;
        for (var i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (ApplyImportedLine(items[i], i + 1)) added++;
            if (i + 1 < items.Count)
                progress?.Report((int)((i + 1L) * 100 / items.Count));
        }
        ct.ThrowIfCancellationRequested();
        progress?.Report(100);
        return added;
    }

    public async Task<int> ImportStreamingAsync(IStreamingOrderLineImporter importer, string path,
        CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();
        progress?.Report(0);
        var added = 0;
        var number = 0;
        await foreach (var item in importer.ReadAsync(path, progress, ct).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            if (ApplyImportedLine(item, ++number)) added++;
        }
        ct.ThrowIfCancellationRequested();
        progress?.Report(100);
        return added;
    }

    private bool ApplyImportedLine(ImportedOrderLine item, int number)
    {
        var order = _store.GetById(item.OrderId)
            ?? throw new DomainException($"Запис {number}: немає замовлення {item.OrderId}.");
        // Ключ імпорту: замовлення + товар. Повторний імпорт не додає дублікатів.
        if (order.Lines.Any(line => string.Equals(line.ProductId, item.Line.ProductId,
                StringComparison.OrdinalIgnoreCase)))
            return false;

        try
        {
            AddLine(order.Id, item.Line.ProductId, item.Line.Name, item.Line.Price, item.Line.Quantity);
        }
        catch (DomainException ex)
        {
            throw new DomainException($"Запис {number}: {ex.Message}", ex);
        }
        return true;
    }

    public Order? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Order> FindAll(Func<Order, bool> predicate) => _store.Find(predicate);

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate) => FindAll(predicate);
}
