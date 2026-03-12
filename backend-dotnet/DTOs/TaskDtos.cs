namespace backend_dotnet.DTOs;

public record CreateTaskRequest(string Title, string? Description);
public record UpdateTaskRequest(string? Title, string? Description, bool? IsCompleted);

public record TaskResponse(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int UserId
);