using System.Net;
using System.Net.Http.Json;

namespace OrderApi.Tests;

public class SecurityTests : IClassFixture<JwtApiFactory>
{
    private const string ProtectedEndpoint = "/orders";
    private readonly JwtApiFactory _factory;

    public SecurityTests(JwtApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Test1_NoToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync(ProtectedEndpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Test2_RandomToken_Returns401()
    {
        var response = await Get(ProtectedEndpoint, "this-is-not-a-jwt");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Test3_ValidToken_Returns200()
    {
        var response = await Get(ProtectedEndpoint, TestJwt.CreateToken());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Test4_WrongIssuer_Returns401()
    {
        var response = await Get(ProtectedEndpoint, TestJwt.CreateToken(issuer: "SomeOtherIssuer"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Test5_WrongAudience_Returns401()
    {
        var response = await Get(ProtectedEndpoint, TestJwt.CreateToken(audience: "SomeOtherAudience"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Test6_ExpiredToken_Returns401()
    {
        // Expired 5 minutes ago: well outside the 30-second clock skew.
        var response = await Get(ProtectedEndpoint, TestJwt.CreateToken(expires: DateTime.UtcNow.AddMinutes(-5)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Test7_WrongSigningKey_Returns401()
    {
        var response = await Get(ProtectedEndpoint, TestJwt.CreateToken(signingKey: "a-completely-different-signing-key-0123456789"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SecurityMe_ReturnsClaimsFromToken()
    {
        var response = await Get("/security/me", TestJwt.CreateToken(userId: "2", username: "user1", role: "User"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(me);
        Assert.Equal("2", me.UserId);
        Assert.Equal("user1", me.Username);
        Assert.Equal("User", me.Role);
    }

    [Fact]
    public async Task SecurityMe_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/security/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task InfrastructureEndpoints_AreAnonymous(string path)
    {
        var response = await _factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<HttpResponseMessage> Get(string path, string token) =>
        _factory.CreateClient().WithBearer(token).GetAsync(path);

    private sealed record MeResponse(string UserId, string Username, string Role);
}
