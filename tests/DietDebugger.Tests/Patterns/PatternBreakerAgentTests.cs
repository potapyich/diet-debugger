using DietDebugger.Application;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;

namespace DietDebugger.Tests.Patterns;

[Trait("Category", "PatternBreakerAgent")]
public class PatternBreakerAgentTests
{
    private static readonly DateOnly Today = new(2026, 3, 24); // Monday
    private readonly PatternBreakerAgent _agent = new();

    private static PatternBreakerContext EmptyCtx(DateOnly? today = null) => new(
        Guid.NewGuid(),
        today ?? Today,
        [],
        [],
        []);

    private static DailySummarySnapshot Summary(
        DateOnly date,
        decimal calories = 1800,
        decimal protein = 60,
        decimal calorieTarget = 2000,
        decimal proteinTarget = 50,
        bool hasBefore10 = true,
        bool hasAfter20 = false) =>
        new(date, calories, protein, calorieTarget, proteinTarget, hasBefore10, hasAfter20);

    // ── weekend_overeating ────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Pattern_WeekendOvereating_Triggers")]
    public void Pattern_WeekendOvereating_Triggers()
    {
        // Weekdays avg ~1800, weekends avg 2400 (133% > 120%)
        var summaries = new List<DailySummarySnapshot>();
        // Two weeks of data
        for (int i = 14; i >= 1; i--)
        {
            var date = Today.AddDays(-i);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            summaries.Add(Summary(date, calories: isWeekend ? 2400 : 1800));
        }

        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], []);
        var results = _agent.Evaluate(ctx);

        Assert.Contains(results, r => r.PatternKey == "weekend_overeating");
    }

    [Fact]
    [Trait("Category", "Pattern_WeekendOvereating_RateLimit_DoesNotRetrigger")]
    public void Pattern_WeekendOvereating_RateLimit_DoesNotRetrigger()
    {
        var summaries = new List<DailySummarySnapshot>();
        for (int i = 14; i >= 1; i--)
        {
            var date = Today.AddDays(-i);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            summaries.Add(Summary(date, calories: isWeekend ? 2400 : 1800));
        }

        // Already fired 1 day ago — within the 3-day rate limit
        var recentEvents = new List<PatternEventSnapshot>
        {
            new("weekend_overeating", DateTime.UtcNow.AddDays(-1))
        };

        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], recentEvents);
        var results = _agent.Evaluate(ctx);

        Assert.DoesNotContain(results, r => r.PatternKey == "weekend_overeating");
    }

    [Fact]
    public void Pattern_WeekendOvereating_DoesNotTrigger_WhenUnderThreshold()
    {
        var summaries = new List<DailySummarySnapshot>();
        for (int i = 14; i >= 1; i--)
        {
            var date = Today.AddDays(-i);
            summaries.Add(Summary(date, calories: 1900)); // same all days
        }

        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], []);
        var results = _agent.Evaluate(ctx);

        Assert.DoesNotContain(results, r => r.PatternKey == "weekend_overeating");
    }

    // ── late_night_calories ───────────────────────────────────────────────────

    [Fact]
    public void Pattern_LateNightCalories_Triggers()
    {
        var meals = new List<MealSnapshot>
        {
            new(Today.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc), 500, 30),
            new(Today.ToDateTime(new TimeOnly(20, 0), DateTimeKind.Utc), 1000, 20), // 67% after 19:00
        };
        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, [], meals, []);
        var results = _agent.Evaluate(ctx);

        Assert.Contains(results, r => r.PatternKey == "late_night_calories");
    }

    [Fact]
    public void Pattern_LateNightCalories_NoTrigger_WhenUnder40Pct()
    {
        var meals = new List<MealSnapshot>
        {
            new(Today.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc), 1500, 60),
            new(Today.ToDateTime(new TimeOnly(20, 0), DateTimeKind.Utc), 300, 10), // 17% after 19:00
        };
        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, [], meals, []);
        var results = _agent.Evaluate(ctx);

        Assert.DoesNotContain(results, r => r.PatternKey == "late_night_calories");
    }

    // ── post_streak_crash ─────────────────────────────────────────────────────

    [Fact]
    public void Pattern_PostStreakCrash_Triggers()
    {
        // 3-day deficit streak, today blows past 130% target
        var summaries = new List<DailySummarySnapshot>
        {
            Summary(Today.AddDays(-3), 1800, calorieTarget: 2000),
            Summary(Today.AddDays(-2), 1700, calorieTarget: 2000),
            Summary(Today.AddDays(-1), 1900, calorieTarget: 2000),
        };
        // Today: 2700 calories (>130% of 2000)
        var meals = new List<MealSnapshot>
        {
            new(Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), 2700, 50)
        };
        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, meals, []);
        var results = _agent.Evaluate(ctx);

        Assert.Contains(results, r => r.PatternKey == "post_streak_crash");
    }

    // ── deficit_streak_N ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Pattern_DeficitStreak_CreatesPositiveEvent")]
    public void Pattern_DeficitStreak_CreatesPositiveEvent()
    {
        var summaries = Enumerable.Range(1, 5)
            .Select(i => Summary(Today.AddDays(-i), 1800, calorieTarget: 2000))
            .ToList();

        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], []);
        var results = _agent.Evaluate(ctx);

        var deficit = results.FirstOrDefault(r => r.PatternKey == "deficit_streak_5");
        Assert.NotNull(deficit);
        Assert.Equal(PatternEventType.Positive, deficit.Type);
    }

    [Fact]
    public void Pattern_DeficitStreak_Fires7NotBelow()
    {
        // 7-day streak — should fire deficit_streak_7, not lower
        var summaries = Enumerable.Range(1, 7)
            .Select(i => Summary(Today.AddDays(-i), 1800, calorieTarget: 2000))
            .ToList();

        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], []);
        var results = _agent.Evaluate(ctx);

        Assert.Contains(results, r => r.PatternKey == "deficit_streak_7");
        Assert.DoesNotContain(results, r => r.PatternKey == "deficit_streak_5");
        Assert.DoesNotContain(results, r => r.PatternKey == "deficit_streak_3");
    }

    // ── first_full_week ───────────────────────────────────────────────────────

    [Fact]
    public void Pattern_FirstFullWeek_Triggers_OnFirstTime()
    {
        var summaries = Enumerable.Range(1, 7)
            .Select(i => Summary(Today.AddDays(-i), 1800, calorieTarget: 2000))
            .ToList();

        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], []);
        var results = _agent.Evaluate(ctx);

        Assert.Contains(results, r => r.PatternKey == "first_full_week");
    }

    [Fact]
    public void Pattern_FirstFullWeek_DoesNotRetrigger()
    {
        var summaries = Enumerable.Range(1, 7)
            .Select(i => Summary(Today.AddDays(-i), 1800, calorieTarget: 2000))
            .ToList();

        // Already fired before
        var events = new List<PatternEventSnapshot>
        {
            new("first_full_week", DateTime.UtcNow.AddDays(-30))
        };

        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], events);
        var results = _agent.Evaluate(ctx);

        Assert.DoesNotContain(results, r => r.PatternKey == "first_full_week");
    }

    // ── protein_streak_3 ──────────────────────────────────────────────────────

    [Fact]
    public void Pattern_ProteinStreak3_Triggers()
    {
        var summaries = new List<DailySummarySnapshot>
        {
            Summary(Today.AddDays(-3), protein: 60, proteinTarget: 50),
            Summary(Today.AddDays(-2), protein: 55, proteinTarget: 50),
            Summary(Today.AddDays(-1), protein: 70, proteinTarget: 50),
        };
        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], []);
        var results = _agent.Evaluate(ctx);

        Assert.Contains(results, r => r.PatternKey == "protein_streak_3");
    }

    // ── no_late_eating_habit ──────────────────────────────────────────────────

    [Fact]
    public void Pattern_NoLateEating_Triggers_FirstTime()
    {
        var summaries = Enumerable.Range(1, 5)
            .Select(i => Summary(Today.AddDays(-i), hasAfter20: false))
            .ToList();

        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], []);
        var results = _agent.Evaluate(ctx);

        Assert.Contains(results, r => r.PatternKey == "no_late_eating_habit");
    }

    [Fact]
    public void Pattern_NoLateEating_DoesNotRetrigger()
    {
        var summaries = Enumerable.Range(1, 5)
            .Select(i => Summary(Today.AddDays(-i), hasAfter20: false))
            .ToList();

        var events = new List<PatternEventSnapshot>
        {
            new("no_late_eating_habit", DateTime.UtcNow.AddDays(-10))
        };

        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], events);
        var results = _agent.Evaluate(ctx);

        Assert.DoesNotContain(results, r => r.PatternKey == "no_late_eating_habit");
    }

    // ── breakfast_skip_compensation ───────────────────────────────────────────

    [Fact]
    public void Pattern_BreakfastSkip_Triggers()
    {
        // 3 days this week without a meal before 10
        var summaries = new List<DailySummarySnapshot>
        {
            Summary(Today.AddDays(-1), hasBefore10: false),
            Summary(Today.AddDays(-2), hasBefore10: false),
            Summary(Today.AddDays(-3), hasBefore10: false),
            Summary(Today.AddDays(-4), hasBefore10: true),
        };
        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], []);
        var results = _agent.Evaluate(ctx);

        Assert.Contains(results, r => r.PatternKey == "breakfast_skip_compensation");
    }

    // ── friday_risk ───────────────────────────────────────────────────────────

    [Fact]
    public void Pattern_FridayRisk_Triggers_OnFriday()
    {
        var friday = new DateOnly(2026, 3, 27); // Friday
        var summaries = new List<DailySummarySnapshot>
        {
            Summary(new DateOnly(2026, 3, 6), calories: 2400, calorieTarget: 2000),  // Friday
            Summary(new DateOnly(2026, 3, 13), calories: 2500, calorieTarget: 2000), // Friday
            Summary(new DateOnly(2026, 3, 20), calories: 2300, calorieTarget: 2000), // Friday
        };
        var ctx = new PatternBreakerContext(Guid.NewGuid(), friday, summaries, [], []);
        var results = _agent.Evaluate(ctx);

        Assert.Contains(results, r => r.PatternKey == "friday_risk");
    }

    [Fact]
    public void Pattern_FridayRisk_DoesNotTrigger_OnNonFriday()
    {
        // Today is Monday
        var summaries = new List<DailySummarySnapshot>
        {
            Summary(new DateOnly(2026, 3, 6), calories: 2400, calorieTarget: 2000),
            Summary(new DateOnly(2026, 3, 13), calories: 2500, calorieTarget: 2000),
        };
        var ctx = new PatternBreakerContext(Guid.NewGuid(), Today, summaries, [], []);
        var results = _agent.Evaluate(ctx);

        Assert.DoesNotContain(results, r => r.PatternKey == "friday_risk");
    }
}
