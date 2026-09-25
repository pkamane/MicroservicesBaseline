namespace AuthApi.Services;

/// <summary>
/// Claim names used in our tokens. ProductApi and OrderApi must use the same names when they validate.
/// </summary>
public static class AuthClaimTypes
{
    public const string Username = "username";
    public const string Role = "role";
}
