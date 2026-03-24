using System.Text;
using System.Text.Json;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DietDebugger.Infrastructure.AI;

public class DailySummaryAgent : IDailySummaryAgent
{
    private readonly string _provider;
    private readonly string _apiKey;
    private readonly ILogger<DailySummaryAgent> _logger;

    public DailySummaryAgent(string provider, string apiKey, ILogger<DailySummaryAgent> logger)
    {
        _provider = provider;
        _apiKey = apiKey;
        _logger = logger;
    }

    public async Task<string[]> GenerateInsightsAsync(
        DailySummaryContext context,
        CancellationToken ct = default)
    {
        var meals = context.Meals;
        var lang = context.UserLanguage ?? "en";
        var totalCals = meals.Sum(m => m.Calories);
        var totalProtein = meals.Sum(m => m.ProteinG);
        var totalFat = meals.Sum(m => m.FatG);
        var totalCarbs = meals.Sum(m => m.CarbsG);

        // Anonymous mode: use generic thresholds
        var calTarget = context.DailyCalorieTarget ?? 2000m;
        var proteinTarget = context.DailyProteinTargetG ?? 50m;
        var fatTarget = context.DailyFatTargetG ?? 65m;

        var personalizationSection = context.IsProfileComplete
            ? $"""
              User targets: {calTarget:F0} kcal, {proteinTarget:F0}g protein, {fatTarget:F0}g fat.
              Please provide personalized analysis based on their specific targets.
              """
            : $"""
              Using generic thresholds: {calTarget:F0} kcal, {proteinTarget:F0}g protein, {fatTarget:F0}g fat.
              Profile not complete — use general healthy eating guidelines.
              """;

        var habitsSection = context.UserHabits is { Count: > 0 }
            ? $"\nUser-reported habits:\n{string.Join("\n", context.UserHabits.Select(h => $"- {h}"))}"
            : string.Empty;

        var prompt = $"""
            Analyze this daily nutrition summary and provide 2-4 honest, direct insights.
            Respond in language: {lang}
            Return only a JSON array of strings, no markdown, no extra text.

            Day summary:
            - Total meals: {meals.Count}
            - Total calories: {totalCals:F0} kcal
            - Total protein: {totalProtein:F1}g
            - Total fat: {totalFat:F1}g
            - Total carbs: {totalCarbs:F1}g

            {personalizationSection}{habitsSection}

            Example response: ["Insight 1", "Insight 2", "Insight 3"]
            """;

        string responseText;

        try
        {
            responseText = _provider.ToLowerInvariant() == "claude"
                ? await CallClaudeAsync(prompt, ct)
                : await CallOpenAiAsync(prompt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DailySummaryAgent failed");
            return ["Unable to generate insights at this time."];
        }

        _logger.LogDebug("DailySummaryAgent response: {Response}", responseText);

        try
        {
            return JsonSerializer.Deserialize<string[]>(responseText) ?? [];
        }
        catch
        {
            return [responseText];
        }
    }

    private async Task<string> CallClaudeAsync(string prompt, CancellationToken ct)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var body = new
        {
            model = "claude-3-5-haiku-20241022",
            max_tokens = 512,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var resp = await httpClient.PostAsync(
            "https://api.anthropic.com/v1/messages",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
            ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "[]";
    }

    private async Task<string> CallOpenAiAsync(string prompt, CancellationToken ct)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var messages = new List<object> { new { role = "user", content = prompt } };
        var body = new { model = "gpt-4o-mini", messages, max_tokens = 512 };

        var resp = await httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
            ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "[]";
    }
}
