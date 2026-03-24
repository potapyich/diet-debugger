using DietDebugger.Application.Auth.Commands;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Application.Auth;

public class AuthService(IAppDbContext db, IJwtService jwtService, IPasswordHasher passwordHasher)
{
    public async Task<AuthResult> RegisterAsync(RegisterCommand cmd, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Email == cmd.Email.ToLower(), ct))
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            Email = cmd.Email.ToLower(),
            PasswordHash = passwordHasher.Hash(cmd.Password)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginCommand cmd, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == cmd.Email.ToLower(), ct)
            ?? throw new InvalidOperationException("Invalid credentials.");

        if (!passwordHasher.Verify(cmd.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid credentials.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> RefreshAsync(RefreshCommand cmd, CancellationToken ct = default)
    {
        var token = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == cmd.RefreshToken, ct)
            ?? throw new InvalidOperationException("Invalid refresh token.");

        if (!token.IsActive)
            throw new InvalidOperationException("Refresh token expired or revoked.");

        token.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await IssueTokensAsync(token.User, ct);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken ct)
    {
        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshTokenValue = jwtService.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync(ct);

        return new AuthResult(accessToken, refreshTokenValue);
    }
}
