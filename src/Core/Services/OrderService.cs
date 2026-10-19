namespace Core.Services;

using Core.Abstractions;
using Core.Domain;
using Core.Events;

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
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");
        order.AddLine(productId, name, price, quantity);
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded);
    }

    public void Confirm(string id)
    {
        var order = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");
        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderConfirmed);
    }

    public void Cancel(string id)
    {
        var order = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");
        order.Cancel();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderCancelled);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Order> FindAll(Func<Order, bool> predicate) => _store.Find(predicate);

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate) => FindAll(predicate);
}
