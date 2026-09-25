using AuthApi.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthApi.Services;

/// <summary>
/// POC ONLY. Fixed in-memory test users, for local learning.
/// Production needs a persistent identity store with user management, lockout, MFA,
/// password policies and auditing (or an external identity provider).
/// </summary>
public sealed class InMemoryUserStore : IUserStore
{
    // Hashes are in the ASP.NET Core PasswordHasher V3 format (PBKDF2-HMAC-SHA512, 100,000 iterations,
    // random 128-bit salt per user). Both test users have the password "password".
    private static readonly UserAccount[] SeedUsers =
    [
        new("1", "admin", "AQAAAAIAAYagAAAAENL9u4Wdj7b4uZLUQv/uQvpUK3brHpSttbCMVIz3klJqrUTpvRkbIXy2c341Pi+x0g==", "Admin"),
        new("2", "user1", "AQAAAAIAAYagAAAAEEARZKQ9DK2h/UPndamXKl5aSA2B4d1++xSP/JE0OB8qeMTzYR2fNGxT30RCwq8ckA==", "User")
    ];

    private readonly Dictionary<string, UserAccount> _users =
        SeedUsers.ToDictionary(user => user.Username, StringComparer.OrdinalIgnoreCase);

    private readonly IPasswordHasher<UserAccount> _passwordHasher;

    public InMemoryUserStore(IPasswordHasher<UserAccount> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public Task<UserAccount?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(username, out var user))
        {
            // Hash anyway so "unknown user" and "wrong password" take about the same time.
            // Otherwise response timing reveals which usernames exist.
            _passwordHasher.VerifyHashedPassword(SeedUsers[0], SeedUsers[0].PasswordHash, password);
            return Task.FromResult<UserAccount?>(null);
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        var isValid = result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;

        return Task.FromResult(isValid ? user : null);
    }
}
