using DietDebugger.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Application;

public class ForecastService(IAppDbContext db)
{
    private const int MinDaysRequired = 7;
    private const decimal KcalPerKg = 7700m;

    public async Task<ForecastResult?> GetForecastAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);

        var goal = await db.UserGoals
            .Where(g => g.UserId == userId)
            .FirstOrDefaultAsync(ct);

        var calorieTarget = goal?.DailyCalorieTarget > 0 ? (decimal)goal.DailyCalorieTarget : 2000m;

        var summaries = await db.DailySummaries
            .Where(s => s.UserId == userId && s.Date >= cutoff)
            .OrderByDescending(s => s.Date)
            .ToListAsync(ct);

        if (summaries.Count < MinDaysRequired)
            return null;

        var deltas = summaries.Select(s => s.TotalCalories - calorieTarget).ToList();
        var avgDailyDelta = deltas.Average();
        var daysUnderTarget = summaries.Count(s => s.TotalCalories <= calorieTarget);
        var deficitConsistencyPercent = (decimal)daysUnderTarget / summaries.Count * 100m;
        var estimatedWeightChange = Math.Round(avgDailyDelta * 30m / KcalPerKg, 1);

        return new ForecastResult(avgDailyDelta, deficitConsistencyPercent, estimatedWeightChange);
    }
}
