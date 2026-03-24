using DietDebugger.Application;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Tests.DailySummary;

using DailySummaryEntity = DietDebugger.Domain.Entities.DailySummary;

public class StubDailySummaryAgent : IDailySummaryAgent
{
    public Task<string[]> GenerateInsightsAsync(DailySummaryContext context, CancellationToken ct = default)
        => Task.FromResult(new[] { "Test insight" });
}

[Trait("Category", "DailySummaryAgent")]
public class MealInvalidationTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (AppDbContext db, User user, DailySummaryService service) Setup()
    {
        var db = CreateDb();
        var user = new User { Email = $"{Guid.NewGuid()}@test.com", PasswordHash = "hash" };
        db.Users.Add(user);
        db.SaveChanges();
        var service = new DailySummaryService(db, new StubDailySummaryAgent());
        return (db, user, service);
    }

    [Fact]
    public async Task MealUpdate_InvalidatesDailySummary()
    {
        var (db, user, service) = Setup();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Generate summary first
        await service.GetOrGenerateAsync(user.Id, today, "en");
        var summary = await db.DailySummaries.FirstAsync(s => s.UserId == user.Id && s.Date == today);
        Assert.False(summary.IsStale);

        // Invalidate (simulates what PUT /meals/{id} does)
        await service.InvalidateAsync(user.Id, today);

        await db.Entry(summary).ReloadAsync();
        Assert.True(summary.IsStale);
    }

    [Fact]
    public async Task MealDelete_InvalidatesDailySummary()
    {
        var (db, user, service) = Setup();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Generate summary first
        await service.GetOrGenerateAsync(user.Id, today, "en");
        var summary = await db.DailySummaries.FirstAsync(s => s.UserId == user.Id && s.Date == today);
        Assert.False(summary.IsStale);

        // Invalidate (simulates what DELETE /meals/{id} does)
        await service.InvalidateAsync(user.Id, today);

        await db.Entry(summary).ReloadAsync();
        Assert.True(summary.IsStale);
    }
}
