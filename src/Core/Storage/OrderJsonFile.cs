using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public static class OrderJsonFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true, // також читаємо старі PascalCase-файли
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static IReadOnlyList<Order> Read(string path)
    {
        var dtos = JsonSerializer.Deserialize<List<OrderDto?>>(File.ReadAllText(path), Options)
            ?? throw new DomainException("JSON має містити масив замовлень, а не null.");
        var orders = new List<Order>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < dtos.Count; i++)
        {
            try
            {
                var dto = dtos[i] ?? throw new DomainException("Замовлення не може бути null.");
                if (dto.Lines is null || dto.Lines.Any(line => line is null))
                    throw new DomainException("lines має бути масивом непорожніх об'єктів.");
                if (!Enum.TryParse<OrderStatus>(dto.Status, out var status) || !Enum.IsDefined(typeof(OrderStatus), status))
                    throw new DomainException("Невідомий статус замовлення.");
                var order = Order.FromDto(dto);
                if (!ids.Add(order.Id)) throw new DomainException($"Повторний id={order.Id}.");
                orders.Add(order);
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                throw new DomainException($"Замовлення {i + 1}: {ex.Message}", ex);
            }
        }
        return orders;
    }

    public static void Write(string path, IEnumerable<Order> orders)
    {
        // Матеріалізуємо до відкриття файлу: помилка переліку не зачепить попередні дані.
        var json = JsonSerializer.Serialize(orders.Select(order => order.ToDto()).ToList(), Options);
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var tmp = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(tmp, json);
            File.Move(tmp, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
