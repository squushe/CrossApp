using Core.Domain;

namespace Api.Contracts;

public sealed record SaveOrderLineRequest(string? ProductId, string? Name, decimal Price, int Quantity);

public sealed record SaveOrderRequest(string? Customer, List<SaveOrderLineRequest?>? Lines)
{
    public Order ToDomain(string id, Order? previous = null)
    {
        // Вхідний DTO не дозволяє клієнту змінити id, час створення або статус.
        var order = Order.Create(id, Customer!);
        foreach (var line in Lines!)
            order.AddLine(line!.ProductId!, line.Name!, line.Price, line.Quantity);
        return previous is null ? order : Order.FromDto(order.ToDto() with
        {
            CreatedAt = previous.CreatedAt,
            Status = previous.Status.ToString()
        });
    }
}
