using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderApi.Models;

namespace OrderApi.Tests;

public class OrderApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrderApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

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
