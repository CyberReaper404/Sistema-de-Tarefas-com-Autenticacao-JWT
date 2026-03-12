using System.Security.Claims;
using backend_dotnet.Data;
using backend_dotnet.DTOs;
using backend_dotnet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TasksController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskResponse>>> List([FromQuery] string status = "all")
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var query = db.Tasks.Where(t => t.UserId == userId.Value);
        status = status.Trim().ToLowerInvariant();

        if (status == "pending")
        {
            query = query.Where(t => !t.IsCompleted);
        }
        else if (status == "completed")
        {
            query = query.Where(t => t.IsCompleted);
        }

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TaskResponse(t.Id, t.Title, t.Description, t.IsCompleted, t.CreatedAt, t.UpdatedAt, t.UserId))
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var title = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest(new { message = "title e obrigatorio" });
        }

        var task = new TodoTask
        {
            Title = title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsCompleted = false,
            UserId = userId.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var response = new TaskResponse(task.Id, task.Title, task.Description, task.IsCompleted, task.CreatedAt, task.UpdatedAt, task.UserId);
        return Created($"/api/tasks/{task.Id}", response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskResponse>> Update(int id, UpdateTaskRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId.Value);
        if (task is null)
        {
            return NotFound(new { message = "tarefa nao encontrada" });
        }

        if (request.Title is not null)
        {
            var title = request.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest(new { message = "title nao pode ficar vazio" });
            }

            task.Title = title;
        }

        if (request.Description is not null)
        {
            task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.IsCompleted.HasValue)
        {
            task.IsCompleted = request.IsCompleted.Value;
        }

        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var response = new TaskResponse(task.Id, task.Title, task.Description, task.IsCompleted, task.CreatedAt, task.UpdatedAt, task.UserId);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId.Value);
        if (task is null)
        {
            return NotFound(new { message = "tarefa nao encontrada" });
        }

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();

        return Ok(new { message = "tarefa removida" });
    }

    private int? GetUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out var id) ? id : null;
    }
}