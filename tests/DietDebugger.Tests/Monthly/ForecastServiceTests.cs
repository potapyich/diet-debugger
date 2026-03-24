using DietDebugger.Application;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

using DailySummaryEntity = DietDebugger.Domain.Entities.DailySummary;

namespace DietDebugger.Tests.Monthly;

[Trait("Category", "ForecastService")]
public class ForecastServiceTests
{
    private static AppDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options);

    private static User CreateUser(AppDbContext db)
    {
        var user = new User { Email = $"u{Guid.NewGuid()}@test.com", PasswordHash = "h" };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    private static void SeedSummaries(AppDbContext db, Guid userId, int days, decimal calories, decimal target = 2000)
    {
        for (int i = 1; i <= days; i++)
        {
            db.DailySummaries.Add(new DailySummaryEntity
            {
                UserId = userId,
                Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-i),
                TotalCalories = calories,
                TotalProteinG = 60,
                TotalFatG = 40,
                TotalCarbsG = 150,
                Insights = "[]"
            });
        }
        if (target != 2000)
        {
            db.UserGoals.Add(new UserGoal
            {
                UserId = userId,
                DailyCalorieTarget = (int)target,
                GoalType = GoalType.Cut
            });
        }
        db.SaveChanges();
    }

    [Fact]
    [Trait("Category", "Forecast_500CalDeficit30Days_Returns1Point9kg")]
    public async Task Forecast_500CalDeficit30Days_Returns1Point9kg()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var user = CreateUser(db);
        SeedSummaries(db, user.Id, 30, 1500, target: 2000); // 500 kcal deficit/day
        var service = new ForecastService(db);

        var result = await service.GetForecastAsync(user.Id);

        Assert.NotNull(result);
        // 500 * 30 / 7700 ≈ 1.9 kg loss
        Assert.Equal(-1.9m, result.EstimatedWeightChangePer30Days);
    }

    [Fact]
    [Trait("Category", "Forecast_Surplus_ReturnsPositiveNumber")]
    public async Task Forecast_Surplus_ReturnsPositiveNumber()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var user = CreateUser(db);
        SeedSummaries(db, user.Id, 10, 2500); // 500 kcal surplus using default 2000 target
        var service = new ForecastService(db);

        var result = await service.GetForecastAsync(user.Id);

        Assert.NotNull(result);
        Assert.True(result.EstimatedWeightChangePer30Days > 0);
    }

    [Fact]
    [Trait("Category", "Forecast_LessThan7Days_ReturnsNull")]
    public async Task Forecast_LessThan7Days_ReturnsNull()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var user = CreateUser(db);
        SeedSummaries(db, user.Id, 5, 1800); // Only 5 days
        var service = new ForecastService(db);

        var result = await service.GetForecastAsync(user.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task Forecast_DeficitConsistencyPercent_IsCorrect()
    {
        using var db = CreateDb(Guid.NewGuid().ToString());
        var user = CreateUser(db);
        // 7 days: 5 under target (1800 < 2000), 2 over (2200 > 2000)
        for (int i = 1; i <= 5; i++)
            db.DailySummaries.Add(new DailySummaryEntity { UserId = user.Id, Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-i), TotalCalories = 1800, TotalProteinG = 60, TotalFatG = 40, TotalCarbsG = 150, Insights = "[]" });
        for (int i = 6; i <= 7; i++)
            db.DailySummaries.Add(new DailySummaryEntity { UserId = user.Id, Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-i), TotalCalories = 2200, TotalProteinG = 60, TotalFatG = 40, TotalCarbsG = 150, Insights = "[]" });
        db.SaveChanges();

        var service = new ForecastService(db);
        var result = await service.GetForecastAsync(user.Id);

        Assert.NotNull(result);
        // 5 out of 7 days ≈ 71.4%
        Assert.True(result.DeficitConsistencyPercent > 70 && result.DeficitConsistencyPercent < 73);
    }
}
