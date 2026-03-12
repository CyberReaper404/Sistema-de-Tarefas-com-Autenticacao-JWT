using System.Security.Claims;
using backend_dotnet.Models;

namespace backend_dotnet.Services;

public record TokenPair(string AccessToken, string RefreshToken, string AccessTokenId, string RefreshTokenId, DateTime RefreshExpiresAt);
public record ValidatedTokenData(int UserId, string TokenId, string TokenType, DateTime ExpiresAtUtc);

public interface ITokenService
{
    TokenPair GenerateTokenPair(User user);
    ValidatedTokenData? ValidateToken(string token, string expectedType, bool validateLifetime = true);
}
