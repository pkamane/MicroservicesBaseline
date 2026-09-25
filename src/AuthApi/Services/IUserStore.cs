using AuthApi.Models;

namespace AuthApi.Services;

/// <summary>
/// Credential store abstraction. The POC uses <see cref="InMemoryUserStore"/>;
/// a real implementation would sit on a persistent identity store.
/// </summary>
public interface IUserStore
{
    /// <summary>Returns the user when the username exists and the password matches; otherwise null.</summary>
    Task<UserAccount?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
}
