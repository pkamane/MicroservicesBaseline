using ProductApi.Models;

namespace ProductApi.Services;

public class ProductService
{
    private readonly List<Product> _products =
    [
        new() { Id = 1, Name = "Keyboard", Price = 49.99m },
        new() { Id = 2, Name = "Mouse", Price = 24.50m },
        new() { Id = 3, Name = "Monitor", Price = 189.00m }
    ];

    private int _nextId = 4;
    private readonly object _lock = new();

    public IReadOnlyList<Product> GetAll()
    {
        lock (_lock)
        {
            return _products.ToList();
        }
    }

    public Product? GetById(int id)
    {
        lock (_lock)
        {
            return _products.FirstOrDefault(product => product.Id == id);
        }
    }

    public Product Create(string name, decimal price)
    {
        lock (_lock)
        {
            var product = new Product
            {
                Id = _nextId++,
                Name = name,
                Price = price
            };

            _products.Add(product);
            return product;
        }
    }
}
