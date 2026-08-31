using ProductApi.Services;

namespace ProductApi.Tests;

public class ProductServiceTests
{
    [Fact]
    public void GetAll_ReturnsSeededProducts()
    {
        var service = new ProductService();

        var products = service.GetAll();

        Assert.NotEmpty(products);
        Assert.Contains(products, product => product.Name == "Keyboard");
    }

    [Fact]
    public void GetById_ReturnsProduct_WhenItExists()
    {
        var service = new ProductService();

        var product = service.GetById(1);

        Assert.NotNull(product);
        Assert.Equal("Keyboard", product.Name);
    }

    [Fact]
    public void GetById_ReturnsNull_WhenMissing()
    {
        var service = new ProductService();

        Assert.Null(service.GetById(999));
    }

    [Fact]
    public void Create_AssignsNewId()
    {
        var service = new ProductService();

        var created = service.Create("Headset", 79.99m);

        Assert.True(created.Id > 0);
        Assert.Equal("Headset", created.Name);
        Assert.Equal(created.Id, service.GetById(created.Id)?.Id);
    }
}
