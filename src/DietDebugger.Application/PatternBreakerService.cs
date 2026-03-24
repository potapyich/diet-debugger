using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Application;

public class PatternBreakerService(IAppDbContext db, IPatternBreakerAgent agent)
{
    public async Task RunAsync(Guid userId, DateOnly today, CancellationToken ct = default)
    {
        var cutoff = today.AddDays(-30);

        // Load last 30 days of summaries with meal timing facts
        var summaryRows = await db.DailySummaries
            .Where(s => s.UserId == userId && s.Date >= cutoff && s.Date < today)
            .OrderByDescending(s => s.Date)
            .ToListAsync(ct);

        // Load meal timing info per date (for HasMealBefore10 / HasMealAfter20)
        var mealsByDate = await db.Meals
            .Where(m => m.UserId == userId && DateOnly.FromDateTime(m.LoggedAt) >= cutoff
                                           && DateOnly.FromDateTime(m.LoggedAt) < today)
            .Select(m => new { Date = DateOnly.FromDateTime(m.LoggedAt), m.LoggedAt, m.Calories, m.ProteinG })
            .ToListAsync(ct);

        // Load user goal for targets
        var goal = await db.UserGoals
            .Where(g => g.UserId == userId)
            .FirstOrDefaultAsync(ct);

        decimal calorieTarget = goal?.DailyCalorieTarget ?? 2000m;
        decimal proteinTarget = goal?.ProteinTargetG ?? 50m;

        var mealsByDateLookup = mealsByDate
            .GroupBy(m => m.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var summarySnapshots = summaryRows.Select(s =>
        {
            mealsByDateLookup.TryGetValue(s.Date, out var dayMeals);
            dayMeals ??= [];
            return new DailySummarySnapshot(
                s.Date,
                s.TotalCalories,
                s.TotalProteinG,
                calorieTarget,
                proteinTarget,
                dayMeals.Any(m => m.LoggedAt.Hour < 10),
                dayMeals.Any(m => m.LoggedAt.Hour >= 20));
        }).ToList();

        // Today's meals
        var todayMeals = await db.Meals
            .Where(m => m.UserId == userId && DateOnly.FromDateTime(m.LoggedAt) == today)
            .Select(m => new { m.LoggedAt, m.Calories, m.ProteinG })
            .ToListAsync(ct);

        var todaySnapshots = todayMeals
            .Select(m => new MealSnapshot(m.LoggedAt, m.Calories, m.ProteinG))
            .ToList();

        // Recent pattern events for rate-limiting
        var eventCutoff = DateTime.UtcNow.AddDays(-30);
        var recentEvents = await db.PatternEvents
            .Where(e => e.UserId == userId && e.TriggeredAt >= eventCutoff)
            .Select(e => new PatternEventSnapshot(e.PatternKey, e.TriggeredAt))
            .ToListAsync(ct);

        // Also load all-time events for "once-only" patterns
        var allEvents = await db.PatternEvents
            .Where(e => e.UserId == userId)
            .Select(e => new PatternEventSnapshot(e.PatternKey, e.TriggeredAt))
            .ToListAsync(ct);

        var habits = await db.UserHabits
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.CreatedAt)
            .Select(h => h.HabitDescription)
            .ToListAsync(ct);

        var brkCtx = new PatternBreakerContext(userId, today, summarySnapshots, todaySnapshots, allEvents, habits);
        var triggered = agent.Evaluate(brkCtx);

        foreach (var candidate in triggered)
        {
            db.PatternEvents.Add(new PatternEvent
            {
                UserId = userId,
                PatternKey = candidate.PatternKey,
                Type = candidate.Type,
                Message = candidate.Message,
                TriggeredAt = DateTime.UtcNow,
                Acknowledged = false
            });
        }

        if (triggered.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
