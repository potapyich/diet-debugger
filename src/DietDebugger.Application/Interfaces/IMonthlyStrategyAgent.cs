using DietDebugger.Domain.Entities;

namespace DietDebugger.Application.Interfaces;

public record ForecastResult(
    decimal AverageDailyDeltaKcal,
    decimal DeficitConsistencyPercent,
    decimal EstimatedWeightChangePer30Days);

public record StrategyForecast(
    decimal EstimatedWeightChangePer30Days,
    decimal DeficitConsistencyPercent,
    string? MainBlocker);

public record StrategyAssessment(string[] Insights, StrategyForecast Forecast);

public record MonthlyStrategyContext(
    IReadOnlyList<WeeklyReport> WeeklyReports,
    User? Profile,
    UserGoal? Goal,
    ForecastResult? Forecast,
    string? UserLanguage);

public interface IMonthlyStrategyAgent
{
    Task<StrategyAssessment> GenerateAsync(MonthlyStrategyContext context, CancellationToken ct = default);
}
