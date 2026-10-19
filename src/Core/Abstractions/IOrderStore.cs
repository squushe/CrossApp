namespace Core.Abstractions;

using Core.Domain;

public interface IOrderStore
{
    IReadOnlyList<Order> List();
    Order? GetById(string id);
    IReadOnlyList<Order> Find(Func<Order, bool> predicate);
    void Add(Order item);
    void Update(Order item);
    bool Remove(string id);
}
