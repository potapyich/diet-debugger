using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

using DailySummaryEntity = DietDebugger.Domain.Entities.DailySummary;

namespace DietDebugger.Tests.DailySummary;

[Trait("Category", "DailySummaryEntity")]
public class DailySummaryEntityTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task DailySummary_CanBeSavedAndRetrieved()
    {
        using var db = CreateDb();
        var user = new User { Email = "ds@test.com", PasswordHash = "hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var summary = new DailySummaryEntity
        {
            UserId = user.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalCalories = 1800,
            TotalProteinG = 90,
            TotalFatG = 60,
            TotalCarbsG = 200,
            Insights = """["Good protein intake","Stay hydrated"]"""
        };
        db.DailySummaries.Add(summary);
        await db.SaveChangesAsync();

        var saved = await db.DailySummaries.FindAsync(summary.Id);
        Assert.NotNull(saved);
        Assert.Equal(1800, saved.TotalCalories);
        Assert.False(saved.IsStale);
    }

    [Fact]
    public async Task DailySummary_IsStale_CanBeUpdated()
    {
        using var db = CreateDb();
        var user = new User { Email = "stale@test.com", PasswordHash = "hash" };
        db.Users.Add(user);
        var summary = new DailySummaryEntity
        {
            UserId = user.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            TotalCalories = 2100
        };
        db.DailySummaries.Add(summary);
        await db.SaveChangesAsync();

        summary.IsStale = true;
        await db.SaveChangesAsync();

        var updated = await db.DailySummaries.FindAsync(summary.Id);
        Assert.True(updated!.IsStale);
    }
}
