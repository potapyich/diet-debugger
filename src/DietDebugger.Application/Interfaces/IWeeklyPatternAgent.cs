using DietDebugger.Domain.Entities;

namespace DietDebugger.Application.Interfaces;

public record CalendarDayStatus(string Date, string Status);

public record WeeklyPatternResult(string[] Patterns, CalendarDayStatus[] CalendarDayStatuses);

public record WeeklyPatternContext(
    IReadOnlyList<DailySummary> DailySummaries,
    string? UserLanguage,
    int? DailyCalorieTarget);

public interface IWeeklyPatternAgent
{
    Task<WeeklyPatternResult> GeneratePatternsAsync(
        WeeklyPatternContext context,
        CancellationToken ct = default);
}
