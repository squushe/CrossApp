using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public sealed class OrderServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddLine_WithInvalidQuantity_ThrowsAndKeepsState(int quantity)
    {
        var order = TestData.Order();
        var store = new InMemoryOrderStore([order]);
        var service = new OrderService(store);
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.AddLine(order.Id, "P-NEW", "Новий товар", 15m, quantity));

        Assert.Single(store.GetById(order.Id)!.Lines);
        Assert.Equal(10m, order.Total);
        Assert.Empty(events);
    }

    [Fact]
    public void AddLine_ToConfirmedOrder_ThrowsDomainExceptionAndKeepsState()
    {
        var order = TestData.Order();
        order.Confirm();
        var service = new OrderService(new InMemoryOrderStore([order]));

        Assert.Throws<DomainException>(
            () => service.AddLine(order.Id, "P-NEW", "Новий товар", 15m, 2));

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Single(order.Lines);
        Assert.Equal(10m, order.Total);
    }

    [Fact]
    public void AddLine_UpdatesTotalAndRaisesChangedExactlyOnce()
    {
        var order = TestData.Order(price: 12.50m, quantity: 2);
        var service = new OrderService(new InMemoryOrderStore([order]));
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.AddLine(order.Id, "P-NEW", "Новий товар", 7m, 3);

        Assert.Equal(46m, order.Total);
        var change = Assert.Single(events);
        Assert.Equal(OrderChangeKind.LineAdded, change.Kind);
        Assert.Equal(order.Id, change.OrderId);
        Assert.Equal(2, change.LinesCount);
        Assert.Equal(46m, change.Total);
    }

    [Fact]
    public void Confirm_EmptyOrder_ThrowsDomainExceptionWithoutEvent()
    {
        var order = Order.Create("EMPTY", "CUSTOMER-1");
        var service = new OrderService(new InMemoryOrderStore([order]));
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        Assert.Throws<DomainException>(() => service.Confirm(order.Id));

        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Empty(events);
    }
}
