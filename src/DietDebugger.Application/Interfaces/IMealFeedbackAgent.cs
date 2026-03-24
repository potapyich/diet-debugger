namespace DietDebugger.Application.Interfaces;

public enum FeedbackStatus { Ok, Warning, Issue }

public record DailyTotals(decimal Calories, decimal ProteinG, decimal FatG, decimal CarbsG);

public record UserNutritionProfile(
    decimal DailyCalorieTarget,
    decimal DailyProteinTargetG,
    decimal DailyFatTargetG);

public record FeedbackResult(FeedbackStatus Status, string[] Insights);

public interface IMealFeedbackAgent
{
    FeedbackResult Evaluate(
        DailyTotals mealNutrition,
        DailyTotals dailyTotalsSoFar,
        UserNutritionProfile? profile);
}
