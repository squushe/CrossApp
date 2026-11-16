using Core.Domain;

namespace Api.Contracts;

public sealed record SaveOrderLineRequest(string? ProductId, string? Name, decimal Price, int Quantity);

public sealed record SaveOrderRequest(string? Customer, List<SaveOrderLineRequest?>? Lines)
{
    public bool TryValidate(out Dictionary<string, string[]> errors)
    {
        errors = new();
        if (string.IsNullOrWhiteSpace(Customer) || Customer.Length > 100)
            errors["customer"] = ["Клієнт обов'язковий, до 100 символів."];
        if (Lines is null || Lines.Count == 0 || Lines.Count > 1000)
            errors["lines"] = ["Замовлення має містити від 1 до 1000 рядків."];
        if (Lines is not null && Lines.Count <= 1000)
        {
            decimal total = 0;
            for (var i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];
                var key = $"lines[{i}]";
                if (line is null)
                {
                    errors[key] = ["Рядок не може бути null."];
                    continue;
                }
                if (string.IsNullOrWhiteSpace(line.ProductId) || line.ProductId.Length > 100)
                    errors[key + ".productId"] = ["ID товару обов'язковий, до 100 символів."];
                if (string.IsNullOrWhiteSpace(line.Name) || line.Name.Length > 200)
                    errors[key + ".name"] = ["Назва обов'язкова, до 200 символів."];
                if (line.Price <= 0)
                    errors[key + ".price"] = ["Ціна має бути більшою за нуль."];
                if (line.Quantity <= 0)
                    errors[key + ".quantity"] = ["Кількість має бути більшою за нуль."];
                try { total += line.Price * line.Quantity; }
                catch (OverflowException) { errors["lines"] = ["Сума замовлення надто велика."]; }
            }
        }
        return errors.Count == 0;
    }

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
