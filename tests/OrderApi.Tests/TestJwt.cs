using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace OrderApi.Tests;

/// <summary>
/// TEST-ONLY token minting that mimics AuthApi's tokens (same claims, issuer, audience, HS256).
/// It lives in the test project only; the resource API itself never issues tokens.
/// </summary>
public static class TestJwt
{
    public const string Issuer = "AuthApi";
    public const string Audience = "Microservices";
    public const string SigningKey = "TEST-ONLY-signing-key-for-integration-tests-0123456789";

    public static string CreateToken(
        string userId = "1",
        string username = "admin",
        string? role = "Admin",
        string issuer = Issuer,
        string audience = Audience,
        string signingKey = SigningKey,
        DateTime? expires = null)
    {
        var now = DateTime.UtcNow;
        var expiresAt = expires ?? now.AddMinutes(60);
        var notBefore = expiresAt <= now ? expiresAt.AddMinutes(-60) : now;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new("username", username)
        };
        if (role is not null)
        {
            claims.Add(new Claim("role", role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = issuer,
            Audience = audience,
            IssuedAt = notBefore,
            NotBefore = notBefore,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static HttpClient WithBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

/// <summary>
/// Runs the API in-memory with a test-only JWT configuration, independent of appsettings files.
/// </summary>
public sealed class JwtApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = TestJwt.Issuer,
            ["Jwt:Audience"] = TestJwt.Audience,
            ["Jwt:SecretKey"] = TestJwt.SigningKey
        }));
    }
}
