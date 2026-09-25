namespace ProductApi.Security;

/// <summary>
/// Role names carried in the JWT "role" claim issued by AuthApi. Values must match AuthApi exactly (case-sensitive).
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
