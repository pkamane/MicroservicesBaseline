namespace OrderApi.Security;

/// <summary>
/// Claim names issued by AuthApi. Must stay identical to AuthApi.Services.AuthClaimTypes.
/// </summary>
public static class AuthClaimTypes
{
    public const string UserId = "sub";
    public const string Username = "username";
    public const string Role = "role";
}
