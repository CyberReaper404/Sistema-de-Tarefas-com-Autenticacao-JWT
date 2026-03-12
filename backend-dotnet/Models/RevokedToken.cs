namespace backend_dotnet.Models;

public class RevokedToken
{
    public int Id { get; set; }
    public string TokenId { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
