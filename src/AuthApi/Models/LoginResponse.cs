namespace AuthApi.Models;

public sealed record LoginResponse(string AccessToken, string TokenType, int ExpiresIn);
