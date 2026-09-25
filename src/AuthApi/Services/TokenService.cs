using System.Security.Claims;
using AuthApi.Configuration;
using AuthApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _jwt;
    private readonly TimeProvider _timeProvider;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public TokenService(IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider)
    {
        _jwt = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public LoginResponse CreateAccessToken(UserAccount user)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var lifetime = TimeSpan.FromMinutes(_jwt.AccessTokenLifetimeMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(AuthClaimTypes.Username, user.Username),
            new Claim(AuthClaimTypes.Role, user.Role)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(lifetime),
            // Symmetric HS256 for the POC: whoever can validate can also sign.
            // Moving to RS256/ES256 (private key here, public key/JWKS in the APIs) removes that risk.
            SigningCredentials = new SigningCredentials(_jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        var token = _tokenHandler.CreateToken(descriptor);

        return new LoginResponse(token, "Bearer", (int)lifetime.TotalSeconds);
    }
}
