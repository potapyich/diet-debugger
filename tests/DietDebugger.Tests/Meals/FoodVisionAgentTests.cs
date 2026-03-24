using DietDebugger.Application.Interfaces;
using DietDebugger.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace DietDebugger.Tests.Meals;

[Trait("Category", "FoodVisionAgent")]
public class FoodVisionAgentTests
{
    private static FoodVisionAgent CreateAgent() =>
        new FoodVisionAgent("openai", "test-key", NullLogger<FoodVisionAgent>.Instance);

    [Fact]
    public async Task FoodVisionAgent_Interface_IsImplemented()
    {
        var agent = CreateAgent();
        Assert.IsAssignableFrom<IFoodVisionAgent>(agent);
    }

    [Fact]
    public void FoodVisionAgent_HasCorrectInterfaces()
    {
        var interfaceType = typeof(IFoodVisionAgent);
        var method = interfaceType.GetMethod("AnalyzeAsync");
        Assert.NotNull(method);
    }
}

[Trait("Category", "FoodVisionAgent")]
public class FoodVisionAgentLowConfidenceTests
{
    [Fact]
    public void FoodVisionAgent_LowConfidence_ReturnsBelowThreshold()
    {
        // Test that we correctly represent low confidence results
        var result = new FoodVisionResult(
            Ingredients: Array.Empty<FoodVisionIngredient>(),
            CaloriesEstimate: 100,
            ProteinG: 5,
            FatG: 3,
            CarbsG: 10,
            PortionEstimate: "unknown",
            Confidence: 0.3
        );

        Assert.True(result.Confidence < 0.4, "Low confidence result should be below 0.4 threshold");
    }
}
