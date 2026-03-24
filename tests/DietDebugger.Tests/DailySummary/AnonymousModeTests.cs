using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;

namespace DietDebugger.Tests.DailySummary;

[Trait("Category", "AnonymousMode")]
public class AnonymousModeTests
{
    public class CapturingAgent : IDailySummaryAgent
    {
        public DailySummaryContext? LastContext { get; private set; }

        public Task<string[]> GenerateInsightsAsync(DailySummaryContext context, CancellationToken ct = default)
        {
            LastContext = context;
            return Task.FromResult(new[] { "Insight" });
        }
    }

    [Fact]
    public async Task DailySummaryAgent_NoProfile_UsesGenericThresholds()
    {
        var agent = new CapturingAgent();

        var context = new DailySummaryContext(
            Meals: new List<Meal>(),
            UserLanguage: "en",
            DailyCalorieTarget: null,
            DailyProteinTargetG: null,
            DailyFatTargetG: null,
            IsProfileComplete: false);

        await agent.GenerateInsightsAsync(context);

        Assert.NotNull(agent.LastContext);
        Assert.False(agent.LastContext!.IsProfileComplete);
        Assert.Null(agent.LastContext.DailyCalorieTarget);
    }

    [Fact]
    public async Task DailySummaryAgent_WithProfile_UsesPersonalizedTargets()
    {
        var agent = new CapturingAgent();

        var context = new DailySummaryContext(
            Meals: new List<Meal>(),
            UserLanguage: "en",
            DailyCalorieTarget: 1800m,
            DailyProteinTargetG: 120m,
            DailyFatTargetG: 60m,
            IsProfileComplete: true);

        await agent.GenerateInsightsAsync(context);

        Assert.NotNull(agent.LastContext);
        Assert.True(agent.LastContext!.IsProfileComplete);
        Assert.Equal(1800m, agent.LastContext.DailyCalorieTarget);
        Assert.Equal(120m, agent.LastContext.DailyProteinTargetG);
    }
}
