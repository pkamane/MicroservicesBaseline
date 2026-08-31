using OrderApi.Models;

namespace OrderApi.Services;

public class OrderService
{
    private readonly List<Order> _orders =
    [
        new() { Id = 1, ProductId = 1, Quantity = 2, CreatedAt = DateTimeOffset.UtcNow },
        new() { Id = 2, ProductId = 3, Quantity = 1, CreatedAt = DateTimeOffset.UtcNow }
    ];

    private int _nextId = 3;
    private readonly object _lock = new();

    public IReadOnlyList<Order> GetAll()
    {
        lock (_lock)
        {
            return _orders.ToList();
        }
    }

    public Order? GetById(int id)
    {
        lock (_lock)
        {
            return _orders.FirstOrDefault(order => order.Id == id);
        }
    }

    public Order Create(int productId, int quantity)
    {
        lock (_lock)
        {
            var order = new Order
            {
                Id = _nextId++,
                ProductId = productId,
                Quantity = quantity,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _orders.Add(order);
            return order;
        }
    }
}
