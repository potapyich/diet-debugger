using DietDebugger.Application.Interfaces;

namespace DietDebugger.Application;

public class MealFeedbackAgent : IMealFeedbackAgent
{
    private const decimal DefaultDailyCalories = 2000m;
    private const decimal DefaultDailyProteinG = 50m;
    private const decimal DefaultDailyFatG = 65m;

    public FeedbackResult Evaluate(
        DailyTotals mealNutrition,
        DailyTotals dailyTotalsSoFar,
        UserNutritionProfile? profile)
    {
        var dailyCalTarget = profile?.DailyCalorieTarget ?? DefaultDailyCalories;
        var dailyProtTarget = profile?.DailyProteinTargetG ?? DefaultDailyProteinG;
        var dailyFatTarget = profile?.DailyFatTargetG ?? DefaultDailyFatG;

        var insights = new List<string>();
        var status = FeedbackStatus.Ok;

        // Rule 1: single meal > 40% daily fat quota → warning
        if (dailyFatTarget > 0 && mealNutrition.FatG > 0.4m * dailyFatTarget)
        {
            insights.Add("This meal is high in fat — over 40% of your daily fat quota.");
            if (status < FeedbackStatus.Warning) status = FeedbackStatus.Warning;
        }

        // Rule 2: meal protein < 10g AND total daily protein < 50% of target → warning
        if (mealNutrition.ProteinG < 10m && dailyTotalsSoFar.ProteinG < 0.5m * dailyProtTarget)
        {
            insights.Add("Low protein intake — try to include more protein sources today.");
            if (status < FeedbackStatus.Warning) status = FeedbackStatus.Warning;
        }

        // Rule 3: meal pushes daily calories > 110% of target → issue
        var totalCalAfterMeal = dailyTotalsSoFar.Calories + mealNutrition.Calories;
        if (dailyCalTarget > 0 && totalCalAfterMeal > 1.1m * dailyCalTarget)
        {
            insights.Add("You're over your daily calorie target.");
            status = FeedbackStatus.Issue;
        }

        // Rule 4: all good
        if (insights.Count == 0)
            insights.Add("Looks good! This meal fits well within your daily targets.");

        return new FeedbackResult(status, insights.Take(2).ToArray());
    }
}
