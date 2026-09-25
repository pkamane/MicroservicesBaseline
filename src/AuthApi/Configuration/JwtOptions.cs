using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Configuration;

/// <summary>
/// JWT settings shared by the token issuer (AuthApi) and, later, every API that validates tokens.
/// Bound from the "Jwt" configuration section and validated at startup (fail fast).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Issuer is required.")]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Audience is required.")]
    public string Audience { get; init; } = string.Empty;

    // HMAC-SHA256 needs a key of at least 256 bits (32 bytes); shorter keys are rejected when signing.
    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:SecretKey is required. Provide it via appsettings.Development.json locally or an environment variable (Jwt__SecretKey).")]
    [MinLength(32, ErrorMessage = "Jwt:SecretKey must be at least 32 characters (256 bits) for HMAC-SHA256.")]
    public string SecretKey { get; init; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Jwt:AccessTokenLifetimeMinutes must be between 1 and 1440.")]
    public int AccessTokenLifetimeMinutes { get; init; } = 60;

    // Single place that turns the secret into a key, so signing and validation can never drift apart.
    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SecretKey));
}
