using Core.Domain;

namespace Core.Services;

public static class OrderService
{
    public static Order CreateOrderForCustomer(string customerId, IReadOnlyList<Order> existingCustomerOrders)
    {
        int draftCount = existingCustomerOrders.Count(o => o.Status == OrderStatus.Draft);
        
        if (draftCount >= 3)
        {
            throw new InvalidOperationException($"Клієнт {customerId} вже має {draftCount} відкритих замовлень. Максимум 3.");
        }

        return Order.Create(Guid.NewGuid().ToString().Substring(0, 5), customerId);
    }
}
