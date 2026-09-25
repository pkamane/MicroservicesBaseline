using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrderApi.Tests;

/// <summary>
/// Role-based authorization: GET /orders (all orders) needs role = Admin;
/// any authenticated user can place an order or read a single order.
/// </summary>
public class RoleAuthorizationTests : IClassFixture<JwtApiFactory>
{
    private readonly JwtApiFactory _factory;

    public RoleAuthorizationTests(JwtApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Case1_ListOrders_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Case2_ListOrders_AsUser_Returns403()
    {
        var response = await ClientFor("User").GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Case3_ListOrders_AsAdmin_Returns200()
    {
        var response = await ClientFor("Admin").GetAsync("/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListOrders_WithTokenButNoRoleClaim_Returns403()
    {
        var response = await ClientFor(role: null).GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_AsUser_Returns200()
    {
        var response = await ClientFor("User").GetAsync("/orders/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_AsUser_Returns201()
    {
        var response = await ClientFor("User").PostAsJsonAsync("/orders", new { productId = 1, quantity = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Forbidden_ReturnsGenericProblemDetails()
    {
        var response = await ClientFor("User").GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertProblemDetails(response, 403, "Forbidden", "You do not have permission to perform this operation.");
    }

    [Fact]
    public async Task Unauthorized_ReturnsProblemDetails_AndKeepsBearerChallenge()
    {
        var response = await _factory.CreateClient().GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
        await AssertProblemDetails(response, 401, "Unauthorized", "Authentication is required. Provide a valid bearer token.");
    }

    private static async Task AssertProblemDetails(HttpResponseMessage response, int status, string title, string detail)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(status, root.GetProperty("status").GetInt32());
        Assert.Equal(title, root.GetProperty("title").GetString());
        Assert.Equal(detail, root.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("traceId").GetString()));
    }

    private HttpClient ClientFor(string? role) =>
        _factory.CreateClient().WithBearer(TestJwt.CreateToken(userId: "2", username: "user1", role: role));
}
