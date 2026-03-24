using System.Text;
using System.Text.Json;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DietDebugger.Infrastructure.AI;

public class WeeklyPatternAgent : IWeeklyPatternAgent
{
    private readonly string _provider;
    private readonly string _apiKey;
    private readonly ILogger<WeeklyPatternAgent> _logger;

    public WeeklyPatternAgent(string provider, string apiKey, ILogger<WeeklyPatternAgent> logger)
    {
        _provider = provider;
        _apiKey = apiKey;
        _logger = logger;
    }

    public async Task<WeeklyPatternResult> GeneratePatternsAsync(
        WeeklyPatternContext context,
        CancellationToken ct = default)
    {
        var calTarget = context.DailyCalorieTarget ?? 2000;
        var summaryLines = context.DailySummaries
            .OrderBy(d => d.Date)
            .Select(d => $"- {d.Date:yyyy-MM-dd}: {d.TotalCalories:F0} kcal, P:{d.TotalProteinG:F0}g, F:{d.TotalFatG:F0}g, C:{d.TotalCarbsG:F0}g");

        // Compute calendar statuses
        var statuses = context.DailySummaries
            .OrderBy(d => d.Date)
            .Select(d =>
            {
                var ratio = calTarget > 0 ? (double)d.TotalCalories / calTarget : 0;
                var status = ratio <= 1.1 ? "green" : ratio <= 1.25 ? "yellow" : "red";
                if (calTarget == 0)
                    status = d.TotalCalories < 2000 ? "green" : d.TotalCalories < 2500 ? "yellow" : "red";
                return new CalendarDayStatus(d.Date.ToString("yyyy-MM-dd"), status);
            }).ToArray();

        var prompt = $"""
            Analyze this weekly nutrition data and return 2-4 behavioral pattern insights.
            Respond in language: {context.UserLanguage ?? "en"}
            Return only a JSON array of strings, no markdown.
            Daily calorie target: {calTarget} kcal

            Weekly data:
            {string.Join('\n', summaryLines)}

            Example: ["Pattern 1", "Pattern 2"]
            """;

        _logger.LogDebug("WeeklyPatternAgent prompt: {Prompt}", prompt);

        string responseText;
        try
        {
            responseText = _provider.ToLowerInvariant() == "claude"
                ? await CallClaudeAsync(prompt, ct)
                : await CallOpenAiAsync(prompt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WeeklyPatternAgent failed");
            return new WeeklyPatternResult(["Unable to generate weekly patterns."], statuses);
        }

        _logger.LogDebug("WeeklyPatternAgent response: {Response}", responseText);

        string[] patterns;
        try
        {
            patterns = JsonSerializer.Deserialize<string[]>(responseText) ?? [];
        }
        catch
        {
            patterns = [responseText];
        }

        return new WeeklyPatternResult(patterns, statuses);
    }

    private async Task<string> CallClaudeAsync(string prompt, CancellationToken ct)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        var body = new { model = "claude-3-5-haiku-20241022", max_tokens = 512, messages = new[] { new { role = "user", content = prompt } } };
        var resp = await httpClient.PostAsync("https://api.anthropic.com/v1/messages",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"), ct);
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
        var resp = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"), ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "[]";
    }
}
