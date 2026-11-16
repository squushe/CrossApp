using Core.Domain;

namespace Api.Contracts;

public sealed record OrderLineDto(string ProductId, string Name, decimal Price, int Quantity);

public sealed record OrderDto(string Id, string Customer, DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLineDto> Lines, string Status, decimal Total)
{
    public static OrderDto FromDomain(Order order) => new(order.Id, order.CustomerId,
        order.CreatedAt, order.Lines.Select(line => new OrderLineDto(
            line.ProductId, line.Name, line.Price, line.Quantity)).ToList(),
        order.Status.ToString(), order.Total);
}

public sealed record OrderTotalDto(string Id, decimal Total);
