using DietDebugger.Application;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Tests.DailySummary;

using DailySummaryEntity = DietDebugger.Domain.Entities.DailySummary;

[Trait("Category", "DailySummaryAgent")]
public class DailySummaryAgentTests
{
    private static AppDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    private static User CreateUser(AppDbContext db, string email)
    {
        var user = new User { Email = email, PasswordHash = "hash" };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public class StubAgent : IDailySummaryAgent
    {
        private int _callCount;
        public int CallCount => _callCount;
        public DailySummaryContext? LastContext { get; private set; }

        public Task<string[]> GenerateInsightsAsync(DailySummaryContext context, CancellationToken ct = default)
        {
            Interlocked.Increment(ref _callCount);
            LastContext = context;
            return Task.FromResult(new[] { "Great job today!" });
        }
    }

    [Fact]
    public async Task DailySummaryAgent_GeneratesInsights()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var user = CreateUser(db, "gen@test.com");
        var agent = new StubAgent();
        var service = new DailySummaryService(db, agent);

        db.Meals.Add(new Meal { UserId = user.Id, Calories = 500, LoggedAt = DateTime.UtcNow });
        db.SaveChanges();

        var summary = await service.GetOrGenerateAsync(user.Id, DateOnly.FromDateTime(DateTime.UtcNow), "en");

        Assert.NotNull(summary);
        Assert.Contains("Great job", summary.Insights);
        Assert.Equal(1, agent.CallCount);
    }

    [Fact]
    public async Task DailySummaryAgent_StaleFlag_TriggersRegeneration()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var user = CreateUser(db, "stale2@test.com");
        var agent = new StubAgent();
        var service = new DailySummaryService(db, agent);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // First generation
        var summary = await service.GetOrGenerateAsync(user.Id, today, "en");
        Assert.Equal(1, agent.CallCount);

        // Invalidate
        await service.InvalidateAsync(user.Id, today);

        // Second call should regenerate
        summary = await service.GetOrGenerateAsync(user.Id, today, "en");
        Assert.Equal(2, agent.CallCount);
        Assert.False(summary.IsStale);
    }

    [Fact]
    [Trait("Category", "DailySummaryAgent_WithHabits")]
    public async Task DailySummaryAgent_WithHabits_IncludesHabitsInPrompt()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var user = CreateUser(db, "habits@test.com");
        var agent = new StubAgent();
        var service = new DailySummaryService(db, agent);

        db.UserHabits.Add(new UserHabit { UserId = user.Id, HabitDescription = "I snack when stressed" });
        db.UserHabits.Add(new UserHabit { UserId = user.Id, HabitDescription = "I skip lunch on Mondays" });
        db.Meals.Add(new Meal { UserId = user.Id, Calories = 500, LoggedAt = DateTime.UtcNow });
        db.SaveChanges();

        await service.GetOrGenerateAsync(user.Id, DateOnly.FromDateTime(DateTime.UtcNow), "en");

        Assert.NotNull(agent.LastContext?.UserHabits);
        Assert.Equal(2, agent.LastContext!.UserHabits!.Count);
        Assert.Contains("I snack when stressed", agent.LastContext.UserHabits);
    }

    [Fact]
    public async Task DailySummaryAgent_ConcurrentRequests_GeneratesOnce()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var user = CreateUser(db, "concurrent@test.com");
        var agent = new StubAgent();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Run 5 concurrent requests - only 1 should call the agent
        var tasks = Enumerable.Range(0, 5).Select(_ =>
        {
            // Each task needs its own db scope to simulate real concurrency
            var service = new DailySummaryService(db, agent);
            return service.GetOrGenerateAsync(user.Id, today, "en");
        });

        await Task.WhenAll(tasks);

        // Due to locking, agent should be called only once
        Assert.Equal(1, agent.CallCount);
    }
}
