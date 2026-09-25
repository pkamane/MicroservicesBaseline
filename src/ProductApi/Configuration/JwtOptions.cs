using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ProductApi.Configuration;

/// <summary>
/// JWT settings used to VALIDATE tokens issued by AuthApi. Same shape and rules as AuthApi's JwtOptions,
/// so issuer and validators are configured identically. Bound from "Jwt" and validated at startup.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Issuer is required.")]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Audience is required.")]
    public string Audience { get; init; } = string.Empty;

    // HS256 (POC): validators must hold the same secret as AuthApi. Later RS256 removes this need.
    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:SecretKey is required. Provide it via appsettings.Development.json / user-secrets locally or an environment variable (Jwt__SecretKey).")]
    [MinLength(32, ErrorMessage = "Jwt:SecretKey must be at least 32 characters (256 bits) for HMAC-SHA256.")]
    public string SecretKey { get; init; } = string.Empty;

    // Not used for validation (the token's own "exp" decides); kept so all services share one config shape.
    [Range(1, 1440, ErrorMessage = "Jwt:AccessTokenLifetimeMinutes must be between 1 and 1440.")]
    public int AccessTokenLifetimeMinutes { get; init; } = 60;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SecretKey));
}
