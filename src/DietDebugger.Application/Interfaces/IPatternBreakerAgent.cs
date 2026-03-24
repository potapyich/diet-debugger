using DietDebugger.Domain.Entities;

namespace DietDebugger.Application.Interfaces;

public record DailySummarySnapshot(
    DateOnly Date,
    decimal TotalCalories,
    decimal TotalProteinG,
    decimal CalorieTarget,
    decimal ProteinTarget,
    bool HasMealBefore10,
    bool HasMealAfter20);

public record MealSnapshot(DateTime LoggedAt, decimal Calories, decimal ProteinG);

public record PatternEventSnapshot(string PatternKey, DateTime TriggeredAt);

public record PatternCandidate(string PatternKey, PatternEventType Type, string Message);

public record PatternBreakerContext(
    Guid UserId,
    DateOnly Today,
    IReadOnlyList<DailySummarySnapshot> RecentSummaries,
    IReadOnlyList<MealSnapshot> TodaysMeals,
    IReadOnlyList<PatternEventSnapshot> RecentPatternEvents,
    IReadOnlyList<string>? UserHabits = null);

public interface IPatternBreakerAgent
{
    IReadOnlyList<PatternCandidate> Evaluate(PatternBreakerContext context);
}
