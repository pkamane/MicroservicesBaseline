using AuthApi.Models;

namespace AuthApi.Services;

public interface ITokenService
{
    LoginResponse CreateAccessToken(UserAccount user);
}
