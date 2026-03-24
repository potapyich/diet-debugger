namespace DietDebugger.Application.Interfaces;

public record FoodVisionIngredient(string Name, string Amount);

public record FoodVisionInput
{
    public byte[]? ImageBytes { get; init; }
    public string? MimeType { get; init; }
    public string? TextDescription { get; init; }
}

public record FoodVisionResult(
    FoodVisionIngredient[] Ingredients,
    decimal CaloriesEstimate,
    decimal ProteinG,
    decimal FatG,
    decimal CarbsG,
    string PortionEstimate,
    double Confidence
);

public interface IFoodVisionAgent
{
    Task<FoodVisionResult> AnalyzeAsync(FoodVisionInput input, CancellationToken ct = default);
}
