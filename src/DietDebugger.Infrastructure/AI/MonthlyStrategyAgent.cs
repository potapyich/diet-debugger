using System.Text;
using System.Text.Json;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DietDebugger.Infrastructure.AI;

public class MonthlyStrategyAgent : IMonthlyStrategyAgent
{
    private readonly string _provider;
    private readonly string _apiKey;
    private readonly ILogger<MonthlyStrategyAgent> _logger;

    public MonthlyStrategyAgent(string provider, string apiKey, ILogger<MonthlyStrategyAgent> logger)
    {
        _provider = provider;
        _apiKey = apiKey;
        _logger = logger;
    }

    public async Task<StrategyAssessment> GenerateAsync(
        MonthlyStrategyContext context,
        CancellationToken ct = default)
    {
        var lang = context.UserLanguage ?? "en";

        var weekSummaries = context.WeeklyReports.Select((w, i) =>
        {
            var patterns = TryDeserializeStringArray(w.Patterns);
            return $"Week {i + 1} ({w.WeekStartDate:MMM dd}): {string.Join("; ", patterns.Take(3))}";
        });

        var forecastSection = context.Forecast != null
            ? $"""
               Current forecast:
               - Avg daily delta: {context.Forecast.AverageDailyDeltaKcal:+0;-0} kcal/day
               - Days under target: {context.Forecast.DeficitConsistencyPercent:F0}%
               - Estimated weight change: {context.Forecast.EstimatedWeightChangePer30Days:+0.0;-0.0} kg/month
               """
            : "Forecast: insufficient data";

        var profileSection = context.Profile != null
            ? $"User: {context.Profile.Age}yo, {context.Profile.WeightKg}kg, goal: {context.Goal?.GoalType.ToString() ?? "unknown"}, target {context.Goal?.DailyCalorieTarget ?? 2000} kcal/day"
            : "Profile: not set";

        var jsonSchema = """{"insights":["..."],"forecast":{"estimatedWeightChangePer30Days":0.0,"deficitConsistencyPercent":0.0,"mainBlocker":"string or null"}}""";
        var weekSummaryText = string.Join("\n", weekSummaries);

        var prompt = $"""
            Analyze this month's nutrition data and provide a strategic assessment.
            Respond in language: {lang}
            Return ONLY a JSON object with this exact structure (no markdown, no extra text):
            {jsonSchema}

            {profileSection}

            Weekly pattern summaries:
            {weekSummaryText}

            {forecastSection}

            Instructions:
            - insights: 3-5 strategic observations (NOT week-by-week recap). Answer: Is this person on track? What is the #1 blocker? What should they change next month?
            - forecast: use the provided numbers, identify the main behavioral blocker (or null if none)
            - Be direct and actionable, not generic
            """;

        try
        {
            var responseText = _provider.ToLowerInvariant() == "claude"
                ? await CallClaudeAsync(prompt, ct)
                : await CallOpenAiAsync(prompt, ct);

            var assessment = JsonSerializer.Deserialize<StrategyAssessment>(responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return assessment ?? FallbackAssessment(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MonthlyStrategyAgent failed");
            return FallbackAssessment(context);
        }
    }

    private static StrategyAssessment FallbackAssessment(MonthlyStrategyContext ctx)
    {
        var weightChange = ctx.Forecast?.EstimatedWeightChangePer30Days ?? 0;
        var consistency = ctx.Forecast?.DeficitConsistencyPercent ?? 0;
        return new StrategyAssessment(
            ["Unable to generate full analysis. Check your weekly patterns for insights."],
            new StrategyForecast(weightChange, consistency, null));
    }

    private static string[] TryDeserializeStringArray(string json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch { return []; }
    }

    private async Task<string> CallClaudeAsync(string prompt, CancellationToken ct)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var body = new
        {
            model = "claude-3-5-haiku-20241022",
            max_tokens = 1024,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var resp = await httpClient.PostAsync(
            "https://api.anthropic.com/v1/messages",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
            ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "{}";
    }

    private async Task<string> CallOpenAiAsync(string prompt, CancellationToken ct)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var messages = new List<object> { new { role = "user", content = prompt } };
        var body = new { model = "gpt-4o-mini", messages, max_tokens = 1024 };

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
            .GetString() ?? "{}";
    }
}
