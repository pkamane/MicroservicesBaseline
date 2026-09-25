namespace AuthApi.Models;

/// <summary>
/// A user as known by the credential store. Holds a password HASH, never the plaintext password.
/// </summary>
public sealed record UserAccount(string UserId, string Username, string PasswordHash, string Role);
