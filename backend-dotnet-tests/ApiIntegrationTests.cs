using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backend_dotnet.Controllers;
using backend_dotnet.Data;
using backend_dotnet.DTOs;
using backend_dotnet.Models;
using backend_dotnet.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace backend_dotnet_tests;

public class ApiIntegrationTests
{
    [Fact]
    public async Task AuthRefreshLogoutFlow_WorksEndToEnd()
    {
        var context = CreateContext();
        var tokenService = CreateTokenService();
        var authController = new AuthController(context, tokenService);

        var registerResult = await authController.Register(new RegisterRequest("Maria", $"maria.{Guid.NewGuid():N}@example.com", "123456"));
        var registerCreated = Assert.IsType<CreatedResult>(registerResult.Result);
        var registerPayload = Assert.IsType<AuthResponse>(registerCreated.Value);

        var tasksController = new TasksController(context)
        {
            ControllerContext = BuildControllerContext(registerPayload.AccessToken)
        };

        var createTaskResult = await tasksController.Create(new CreateTaskRequest("Tarefa de teste", "Fluxo completo"));
        Assert.IsType<CreatedResult>(createTaskResult.Result);

        var refreshResult = await authController.Refresh(new RefreshRequest(registerPayload.RefreshToken));
        var refreshOk = Assert.IsType<OkObjectResult>(refreshResult.Result);
        var refreshPayload = Assert.IsType<AuthResponse>(refreshOk.Value);

        authController.ControllerContext = BuildControllerContext(refreshPayload.AccessToken);
        var logoutResult = await authController.Logout(new RefreshRequest(refreshPayload.RefreshToken));
        Assert.IsType<OkObjectResult>(logoutResult);

        var revokedTokens = await context.RevokedTokens.ToListAsync();
        Assert.NotEmpty(revokedTokens);

        var revokedRefresh = await context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenId == ReadTokenId(refreshPayload.RefreshToken));
        Assert.NotNull(revokedRefresh);
        Assert.NotNull(revokedRefresh!.RevokedAt);
    }

    [Fact]
    public async Task Tasks_AreIsolatedByUser()
    {
        var context = CreateContext();
        var tokenService = CreateTokenService();
        var authController = new AuthController(context, tokenService);

        var firstUserRegister = await authController.Register(new RegisterRequest("Maria", $"maria.{Guid.NewGuid():N}@example.com", "123456"));
        var firstCreated = Assert.IsType<CreatedResult>(firstUserRegister.Result);
        var firstPayload = Assert.IsType<AuthResponse>(firstCreated.Value);

        var secondUserRegister = await authController.Register(new RegisterRequest("Ana", $"ana.{Guid.NewGuid():N}@example.com", "123456"));
        var secondCreated = Assert.IsType<CreatedResult>(secondUserRegister.Result);
        var secondPayload = Assert.IsType<AuthResponse>(secondCreated.Value);

        var firstTasksController = new TasksController(context)
        {
            ControllerContext = BuildControllerContext(firstPayload.AccessToken)
        };

        var createTaskResult = await firstTasksController.Create(new CreateTaskRequest("Privada", "Somente usuario 1"));
        Assert.IsType<CreatedResult>(createTaskResult.Result);

        var secondTasksController = new TasksController(context)
        {
            ControllerContext = BuildControllerContext(secondPayload.AccessToken)
        };

        var secondUserList = await secondTasksController.List();
        var secondUserOk = Assert.IsType<OkObjectResult>(secondUserList.Result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskResponse>>(secondUserOk.Value);
        Assert.Empty(tasks);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TodoApiTests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    private static ITokenService CreateTokenService()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "testing-jwt-secret-key-with-at-least-32-characters",
            ["Jwt:Issuer"] = "TodoApiTests",
            ["Jwt:Audience"] = "TodoApiTestsUsers",
            ["Jwt:AccessTokenMinutes"] = "30",
            ["Jwt:RefreshTokenDays"] = "7"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new TokenService(configuration);
    }

    private static ControllerContext BuildControllerContext(string accessToken)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var identity = new ClaimsIdentity(token.Claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    private static string ReadTokenId(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
    }
}
