using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backend_dotnet.Data;
using backend_dotnet.DTOs;
using backend_dotnet.Models;
using backend_dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, ITokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new { message = "name, email e password sao obrigatorios" });
        }

        var exists = await db.Users.AnyAsync(u => u.Email == email);
        if (exists)
        {
            return Conflict(new { message = "email ja cadastrado" });
        }

        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await CreateAuthResponse(user);
        return Created(string.Empty, response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new { message = "email e password sao obrigatorios" });
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return Unauthorized(new { message = "credenciais invalidas" });
        }

        var response = await CreateAuthResponse(user);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "refresh token obrigatorio" });
        }

        var validated = tokenService.ValidateToken(request.RefreshToken, "refresh");
        if (validated is null)
        {
            return Unauthorized(new { message = "refresh token invalido" });
        }

        var refreshRecord = await db.RefreshTokens.FirstOrDefaultAsync(t =>
            t.TokenId == validated.TokenId &&
            t.UserId == validated.UserId);

        if (refreshRecord is null || refreshRecord.RevokedAt is not null || refreshRecord.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "refresh token invalido" });
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == validated.UserId);
        if (user is null)
        {
            return Unauthorized(new { message = "usuario nao encontrado" });
        }

        refreshRecord.RevokedAt = DateTime.UtcNow;
        db.RevokedTokens.Add(new RevokedToken
        {
            TokenId = validated.TokenId,
            TokenType = "refresh",
            UserId = user.Id,
            RevokedAt = DateTime.UtcNow
        });

        var tokenPair = tokenService.GenerateTokenPair(user);

        db.RefreshTokens.Add(new RefreshToken
        {
            TokenId = tokenPair.RefreshTokenId,
            UserId = user.Id,
            ExpiresAt = tokenPair.RefreshExpiresAt,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var response = new AuthResponse(
            tokenPair.AccessToken,
            tokenPair.RefreshToken,
            new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt)
        );

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout(RefreshRequest? request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var accessTokenId = User.FindFirstValue(JwtRegisteredClaimNames.Jti);

        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(accessTokenId))
        {
            return Unauthorized(new { message = "token invalido" });
        }

        var accessTokenAlreadyRevoked = await db.RevokedTokens.AnyAsync(t => t.TokenId == accessTokenId);
        if (!accessTokenAlreadyRevoked)
        {
            db.RevokedTokens.Add(new RevokedToken
            {
                TokenId = accessTokenId,
                TokenType = "access",
                UserId = userId,
                RevokedAt = DateTime.UtcNow
            });
        }

        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            var refreshValidation = tokenService.ValidateToken(request.RefreshToken, "refresh", validateLifetime: false);
            if (refreshValidation is not null && refreshValidation.UserId == userId)
            {
                var refreshRecord = await db.RefreshTokens.FirstOrDefaultAsync(t =>
                    t.TokenId == refreshValidation.TokenId &&
                    t.UserId == userId);

                if (refreshRecord is not null && refreshRecord.RevokedAt is null)
                {
                    refreshRecord.RevokedAt = DateTime.UtcNow;
                }

                var refreshAlreadyRevoked = await db.RevokedTokens.AnyAsync(t => t.TokenId == refreshValidation.TokenId);
                if (!refreshAlreadyRevoked)
                {
                    db.RevokedTokens.Add(new RevokedToken
                    {
                        TokenId = refreshValidation.TokenId,
                        TokenType = "refresh",
                        UserId = userId,
                        RevokedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await db.SaveChangesAsync();
        return Ok(new { message = "logout realizado" });
    }

    private async Task<AuthResponse> CreateAuthResponse(User user)
    {
        var tokenPair = tokenService.GenerateTokenPair(user);

        db.RefreshTokens.Add(new RefreshToken
        {
            TokenId = tokenPair.RefreshTokenId,
            UserId = user.Id,
            ExpiresAt = tokenPair.RefreshExpiresAt,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        return new AuthResponse(
            tokenPair.AccessToken,
            tokenPair.RefreshToken,
            new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt)
        );
    }
}
