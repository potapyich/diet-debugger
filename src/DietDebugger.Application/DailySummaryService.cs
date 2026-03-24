using System.Collections.Concurrent;
using System.Text.Json;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Application;

public class DailySummaryService(IAppDbContext db, IDailySummaryAgent agent)
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<DailySummary> GetOrGenerateAsync(
        Guid userId,
        DateOnly date,
        string? userLanguage,
        CancellationToken ct = default)
    {
        var key = $"{userId}:{date}";
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await sem.WaitAsync(ct);
        try
        {
            var summary = await db.DailySummaries
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == date, ct);

            if (summary != null && !summary.IsStale)
                return summary;

            var meals = await db.Meals
                .Where(m => m.UserId == userId
                    && m.LoggedAt >= date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                    && m.LoggedAt < date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1))
                .ToListAsync(ct);

            var habits = await db.UserHabits
                .Where(h => h.UserId == userId)
                .OrderBy(h => h.CreatedAt)
                .Select(h => h.HabitDescription)
                .ToListAsync(ct);

            var ctx = new DailySummaryContext(
                meals, userLanguage,
                DailyCalorieTarget: null, DailyProteinTargetG: null, DailyFatTargetG: null,
                IsProfileComplete: false,
                UserHabits: habits);
            var insights = await agent.GenerateInsightsAsync(ctx, ct);

            if (summary == null)
            {
                summary = new DailySummary
                {
                    UserId = userId,
                    Date = date,
                    TotalCalories = meals.Sum(m => m.Calories),
                    TotalProteinG = meals.Sum(m => m.ProteinG),
                    TotalFatG = meals.Sum(m => m.FatG),
                    TotalCarbsG = meals.Sum(m => m.CarbsG),
                    Insights = JsonSerializer.Serialize(insights),
                    GeneratedAt = DateTime.UtcNow,
                    IsStale = false
                };
                db.DailySummaries.Add(summary);
            }
            else
            {
                summary.TotalCalories = meals.Sum(m => m.Calories);
                summary.TotalProteinG = meals.Sum(m => m.ProteinG);
                summary.TotalFatG = meals.Sum(m => m.FatG);
                summary.TotalCarbsG = meals.Sum(m => m.CarbsG);
                summary.Insights = JsonSerializer.Serialize(insights);
                summary.GeneratedAt = DateTime.UtcNow;
                summary.IsStale = false;
            }

            await db.SaveChangesAsync(ct);
            return summary;
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task InvalidateAsync(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var summary = await db.DailySummaries
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == date, ct);
        if (summary != null)
        {
            summary.IsStale = true;
            await db.SaveChangesAsync(ct);
        }
    }
}
