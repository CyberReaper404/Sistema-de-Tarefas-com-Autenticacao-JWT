using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend_dotnet.Models;
using Microsoft.IdentityModel.Tokens;

namespace backend_dotnet.Services;

public class TokenService(IConfiguration configuration) : ITokenService
{
    private readonly string _key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key nao configurada");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer nao configurado");
    private readonly string _audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience nao configurada");
    private readonly int _accessTokenMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var access) ? access : 30;
    private readonly int _refreshTokenDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out var refresh) ? refresh : 7;

    public TokenPair GenerateTokenPair(User user)
    {
        var accessTokenId = Guid.NewGuid().ToString();
        var refreshTokenId = Guid.NewGuid().ToString();

        var accessExpiresAt = DateTime.UtcNow.AddMinutes(_accessTokenMinutes);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays);

        var accessClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, accessTokenId),
            new("token_type", "access")
        };

        var refreshClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, refreshTokenId),
            new("token_type", "refresh")
        };

        var accessToken = CreateToken(accessClaims, accessExpiresAt);
        var refreshToken = CreateToken(refreshClaims, refreshExpiresAt);

        return new TokenPair(accessToken, refreshToken, accessTokenId, refreshTokenId, refreshExpiresAt);
    }

    public ValidatedTokenData? ValidateToken(string token, string expectedType, bool validateLifetime = true)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = validateLifetime,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var jwtToken = validatedToken as JwtSecurityToken;
            if (jwtToken is null)
            {
                return null;
            }

            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var tokenId = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var tokenType = principal.FindFirstValue("token_type");

            if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(tokenId) || string.IsNullOrWhiteSpace(tokenType))
            {
                return null;
            }

            if (!string.Equals(tokenType, expectedType, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new ValidatedTokenData(userId, tokenId, tokenType, jwtToken.ValidTo.ToUniversalTime());
        }
        catch
        {
            return null;
        }
    }

    private string CreateToken(IEnumerable<Claim> claims, DateTime expiresAt)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
