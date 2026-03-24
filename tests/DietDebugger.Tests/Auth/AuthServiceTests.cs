using DietDebugger.Application.Auth;
using DietDebugger.Application.Auth.Commands;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Xunit;

namespace DietDebugger.Tests.Auth;

[Trait("Category", "Auth")]
public class AuthServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AuthService CreateService(IAppDbContext db)
    {
        var jwtService = new FakeJwtService();
        var hasher = new FakePasswordHasher();
        return new AuthService(db, jwtService, hasher);
    }

    [Fact]
    public async Task Register_ValidCredentials_ReturnsTokens()
    {
        using var db = CreateInMemoryDb();
        var service = CreateService(db);

        var result = await service.RegisterAsync(new RegisterCommand("test@example.com", "Password1!"));

        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        using var db = CreateInMemoryDb();
        var service = CreateService(db);

        await service.RegisterAsync(new RegisterCommand("dupe@example.com", "Password1!"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new RegisterCommand("dupe@example.com", "Password1!")));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        using var db = CreateInMemoryDb();
        var service = CreateService(db);

        await service.RegisterAsync(new RegisterCommand("login@example.com", "Password1!"));
        var result = await service.LoginAsync(new LoginCommand("login@example.com", "Password1!"));

        Assert.NotEmpty(result.AccessToken);
    }

    [Fact]
    public async Task Login_WrongPassword_Throws()
    {
        using var db = CreateInMemoryDb();
        var service = CreateService(db);

        await service.RegisterAsync(new RegisterCommand("wrong@example.com", "Password1!"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync(new LoginCommand("wrong@example.com", "WrongPassword!")));
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        using var db = CreateInMemoryDb();
        var service = CreateService(db);

        var auth = await service.RegisterAsync(new RegisterCommand("refresh@example.com", "Password1!"));
        var result = await service.RefreshAsync(new RefreshCommand(auth.RefreshToken));

        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.NotEqual(auth.RefreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidToken_Throws()
    {
        using var db = CreateInMemoryDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RefreshAsync(new RefreshCommand("invalid-token")));
    }
}

internal class FakeJwtService : IJwtService
{
    public string GenerateAccessToken(User user) => $"fake-access-{user.Id}";
    public string GenerateRefreshToken() => Guid.NewGuid().ToString();
}

internal class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";
    public bool Verify(string password, string hash) => hash == $"hashed:{password}";
}
