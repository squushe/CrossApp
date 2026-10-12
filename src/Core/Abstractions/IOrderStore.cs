namespace Core.Abstractions;

using Core.Domain;

public interface IOrderStore
{
    IReadOnlyList<Order> List();
    Order? GetById(string id);
    void Add(Order item);
    void Update(Order item);
    bool Remove(string id);
}
