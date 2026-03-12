namespace backend_dotnet.DTOs;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);

public record UserResponse(int Id, string Name, string Email, DateTime CreatedAt);

public record AuthResponse(string AccessToken, string RefreshToken, UserResponse User)
{
    public string Token => AccessToken;
}
