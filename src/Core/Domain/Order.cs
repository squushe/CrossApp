using Core.Dto;

namespace Core.Domain;

public enum OrderStatus
{
    Draft,
    Confirmed,
    Cancelled
}

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    
    public OrderStatus Status { get; private set; }
    
    public decimal Total => _lines.Sum(l => l.Price * l.Quantity);

    private Order(string id, string customerId, OrderStatus status)
    {
        Id = id;
        CustomerId = customerId;
        Status = status;
    }

    public static Order Create(string id, string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор замовлення обов'язковий", nameof(id));
            
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Ідентифікатор клієнта обов'язковий", nameof(customerId));

        return new Order(id.Trim(), customerId.Trim(), OrderStatus.Draft);
    }

    public void AddLine(string productId, string name, decimal price, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Замовлення {Id} має статус {Status}, рядки можна додавати лише в стані Draft");

        var line = OrderLine.Create(productId, name, price, quantity);
        _lines.Add(line);
    }

    public void Confirm()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException($"Неможливо підтвердити порожнє замовлення {Id}");
            
        Status = Status switch
        {
            OrderStatus.Draft => OrderStatus.Confirmed,
            OrderStatus.Confirmed => throw new InvalidOperationException($"Замовлення {Id} вже підтверджено"),
            OrderStatus.Cancelled => throw new InvalidOperationException($"Не можна підтвердити скасоване замовлення {Id}"),
            _ => throw new InvalidOperationException("Невідомий стан")
        };
    }
    
    public void Cancel()
    {
        Status = Status switch
        {
            OrderStatus.Draft => OrderStatus.Cancelled,
            OrderStatus.Confirmed => throw new InvalidOperationException($"Замовлення {Id} вже підтверджено, скасування неможливе"),
            OrderStatus.Cancelled => throw new InvalidOperationException($"Замовлення {Id} вже скасоване"),
            _ => throw new InvalidOperationException("Невідомий стан")
        };
    }

    public OrderDto ToDto() => 
        new(Id, CustomerId, _lines.Select(l => l.ToDto()).ToList(), Status.ToString());

    public static Order FromDto(OrderDto dto)
    {
        var order = Create(dto.Id, dto.CustomerId);
        
        foreach (var lineDto in dto.Lines)
        {
            order.AddLine(lineDto.ProductId, lineDto.Name, lineDto.Price, lineDto.Quantity);
        }
        
        if (Enum.TryParse<OrderStatus>(dto.Status, out var status))
        {
            if (status == OrderStatus.Confirmed)
                order.Confirm();
            else if (status == OrderStatus.Cancelled)
                order.Cancel();
        }
        
        return order;
    }

    public override string ToString() => 
        $"Замовлення {Id} (Клієнт {CustomerId}) — {Status}, Сума: {Total:F2}, Рядків: {Lines.Count}";
}
