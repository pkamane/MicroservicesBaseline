using OrderApi.Services;

namespace OrderApi.Tests;

public class OrderServiceTests
{
    [Fact]
    public void GetAll_ReturnsSeededOrders()
    {
        var service = new OrderService();

        Assert.NotEmpty(service.GetAll());
    }

    [Fact]
    public void GetById_ReturnsOrder_WhenItExists()
    {
        var service = new OrderService();

        var order = service.GetById(1);

        Assert.NotNull(order);
        Assert.Equal(1, order.ProductId);
    }

    [Fact]
    public void Create_StoresOrder()
    {
        var service = new OrderService();

        var created = service.Create(2, 4);

        Assert.Equal(2, created.ProductId);
        Assert.Equal(4, created.Quantity);
        Assert.NotNull(service.GetById(created.Id));
    }
}
