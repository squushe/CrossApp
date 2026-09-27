using Core.Dto;

namespace Core.Domain;

public sealed class OrderLine
{
    public string ProductId { get; }
    public string Name { get; }
    public decimal Price { get; }
    public int Quantity { get; }

    private OrderLine(string productId, string name, decimal price, int quantity)
    {
        ProductId = productId;
        Name = name;
        Price = price;
        Quantity = quantity;
    }

    public static OrderLine Create(string productId, string name, decimal price, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("ID товару обов'язковий", nameof(productId));
            
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва товару не може бути порожньою", nameof(name));
            
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price, "Ціна товару не може бути від'ємною");
            
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Кількість має бути більшою за нуль");

        return new OrderLine(productId.Trim(), name.Trim(), price, quantity);
    }
    
    public OrderLineDto ToDto() => new(ProductId, Name, Price, Quantity);
    
    public static OrderLine FromDto(OrderLineDto dto) => 
        Create(dto.ProductId, dto.Name, dto.Price, dto.Quantity);
}
