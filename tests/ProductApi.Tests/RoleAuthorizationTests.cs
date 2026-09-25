using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProductApi.Tests;

/// <summary>
/// Role-based authorization: reads need any valid JWT; POST /products needs role = Admin.
/// </summary>
public class RoleAuthorizationTests : IClassFixture<JwtApiFactory>
{
    private readonly JwtApiFactory _factory;

    public RoleAuthorizationTests(JwtApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Case1_CreateProduct_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/products", NewProduct());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Case2_CreateProduct_AsUser_Returns403()
    {
        var response = await ClientFor("User").PostAsJsonAsync("/products", NewProduct());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Case3_CreateProduct_AsAdmin_Returns201()
    {
        var response = await ClientFor("Admin").PostAsJsonAsync("/products", NewProduct());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithTokenButNoRoleClaim_Returns403()
    {
        var response = await ClientFor(role: null).PostAsJsonAsync("/products", NewProduct());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/products")]
    [InlineData("/products/1")]
    public async Task ReadEndpoints_AsUser_Return200(string path)
    {
        var response = await ClientFor("User").GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Forbidden_ReturnsGenericProblemDetails()
    {
        var response = await ClientFor("User").PostAsJsonAsync("/products", NewProduct());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertProblemDetails(response, 403, "Forbidden", "You do not have permission to perform this operation.");
    }

    [Fact]
    public async Task Unauthorized_ReturnsProblemDetails_AndKeepsBearerChallenge()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/products", NewProduct());

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

    private static object NewProduct() => new { name = "Headset", price = 79.99m };
}
