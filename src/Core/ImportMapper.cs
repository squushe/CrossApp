using Core.Domain;
using Core.Dto;

namespace Core;

public static class ImportMapper
{
    public static (IReadOnlyList<Order> ValidOrders, IReadOnlyList<string> Errors) MapOrders(ImportResult<DomainRecord> importResult)
    {
        var validOrders = new List<Order>();
        var errors = new List<string>(importResult.Errors);

        foreach (var record in importResult.Items)
        {
            if (record is OrderDto dto)
            {
                try
                {
                    validOrders.Add(Order.FromDto(dto));
                }
                catch (Exception ex)
                {
                    errors.Add($"Помилка валідації замовлення {dto.Id}: {ex.Message}");
                }
            }
        }

        return (validOrders, errors);
    }
}
