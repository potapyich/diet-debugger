using DietDebugger.Domain.Entities;

namespace DietDebugger.Application.Interfaces;

public record DailySummaryContext(
    IReadOnlyList<Meal> Meals,
    string? UserLanguage,
    // Null = anonymous mode (use generic thresholds)
    decimal? DailyCalorieTarget,
    decimal? DailyProteinTargetG,
    decimal? DailyFatTargetG,
    bool IsProfileComplete,
    IReadOnlyList<string>? UserHabits = null);

public interface IDailySummaryAgent
{
    Task<string[]> GenerateInsightsAsync(
        DailySummaryContext context,
        CancellationToken ct = default);
}
