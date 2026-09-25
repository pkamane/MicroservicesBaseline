using System.Net;
using System.Net.Http.Json;
using OrderApi.Models;

namespace OrderApi.Tests;

public class OrderApiTests : IClassFixture<JwtApiFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _anonymousClient;

    public OrderApiTests(JwtApiFactory factory)
    {
        // Business endpoints now require a JWT; health stays anonymous.
        _client = factory.CreateClient().WithBearer(TestJwt.CreateToken());
        _anonymousClient = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _anonymousClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_ReturnsSeededOrders()
    {
        var orders = await _client.GetFromJsonAsync<List<Order>>("/orders");

        Assert.NotNull(orders);
        Assert.NotEmpty(orders);
    }

    [Fact]
    public async Task CreateOrder_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/orders", new { productId = 2, quantity = 3 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(created);
        Assert.Equal(2, created.ProductId);
        Assert.Equal(3, created.Quantity);
    }

    [Fact]
    public async Task CreateOrder_ReturnsBadRequest_WhenQuantityIsInvalid()
    {
        var response = await _client.PostAsJsonAsync("/orders", new { productId = 1, quantity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
