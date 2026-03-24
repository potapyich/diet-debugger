using DietDebugger.Application;
using DietDebugger.Application.Interfaces;

namespace DietDebugger.Tests.Meals;

[Trait("Category", "MealFeedbackAgent")]
public class MealFeedbackAgentTests
{
    private static readonly MealFeedbackAgent Agent = new();
    private static readonly UserNutritionProfile Profile = new(2000m, 50m, 65m);
    private static readonly DailyTotals EmptyDay = new(0, 0, 0, 0);

    [Fact]
    public void MealFeedback_BalancedMeal_ReturnsOk()
    {
        var meal = new DailyTotals(400, 30, 15, 50);
        var result = Agent.Evaluate(meal, EmptyDay, Profile);
        Assert.Equal(FeedbackStatus.Ok, result.Status);
    }

    [Fact]
    public void MealFeedback_HighFatMeal_ReturnsWarning()
    {
        // 40% of 65g = 26g, so 30g triggers warning
        var meal = new DailyTotals(400, 30, 30, 50);
        var result = Agent.Evaluate(meal, EmptyDay, Profile);
        Assert.Equal(FeedbackStatus.Warning, result.Status);
        Assert.Contains(result.Insights, i => i.Contains("fat"));
    }

    [Fact]
    public void MealFeedback_LowProteinMeal_ReturnsWarning()
    {
        // meal protein < 10g AND total daily protein < 25g (50% of 50g)
        var meal = new DailyTotals(300, 5, 10, 50);
        var result = Agent.Evaluate(meal, EmptyDay, Profile);
        Assert.Equal(FeedbackStatus.Warning, result.Status);
        Assert.Contains(result.Insights, i => i.Contains("protein") || i.ToLower().Contains("protein"));
    }

    [Fact]
    public void MealFeedback_OverCalorieTarget_ReturnsIssue()
    {
        // daily so far 1900, meal adds 400 → total 2300 > 2200 (110% of 2000)
        var meal = new DailyTotals(400, 30, 15, 50);
        var dayTotal = new DailyTotals(1900, 40, 30, 200);
        var result = Agent.Evaluate(meal, dayTotal, Profile);
        Assert.Equal(FeedbackStatus.Issue, result.Status);
    }

    [Fact]
    public void MealFeedback_NullProfile_UsesDefaults()
    {
        // With defaults: 2000kcal, 50g protein, 65g fat
        // High fat: 30g > 40% of 65g (26g) → warning
        var meal = new DailyTotals(400, 30, 30, 50);
        var result = Agent.Evaluate(meal, EmptyDay, null);
        Assert.Equal(FeedbackStatus.Warning, result.Status);
    }
}
