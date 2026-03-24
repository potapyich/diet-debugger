using System.Text.Json;
using System.Text.Json.Serialization;
using DietDebugger.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DietDebugger.Infrastructure.AI;

public class FoodVisionAgent : IFoodVisionAgent
{
    private readonly string _provider;
    private readonly string _apiKey;
    private readonly ILogger<FoodVisionAgent> _logger;

    private static readonly string PromptText = """
        Analyze the food in this image (or description) and return a JSON object with this exact structure:
        {
          "ingredients": [{"name": "string", "amount": "string"}],
          "caloriesEstimate": 0,
          "proteinG": 0,
          "fatG": 0,
          "carbsG": 0,
          "portionEstimate": "string",
          "confidence": 0.0
        }
        Return only the JSON object, no markdown, no explanation.
        confidence is between 0 and 1. Use low confidence (< 0.4) if unclear.
        """;

    public FoodVisionAgent(string provider, string apiKey, ILogger<FoodVisionAgent> logger)
    {
        _provider = provider;
        _apiKey = apiKey;
        _logger = logger;
    }

    public async Task<FoodVisionResult> AnalyzeAsync(FoodVisionInput input, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        string responseText = _provider.ToLowerInvariant() switch
        {
            "claude" => await CallClaudeAsync(input, cts.Token),
            _ => await CallOpenAiAsync(input, cts.Token)
        };

        _logger.LogDebug("FoodVisionAgent response: {Response}", responseText);

        return ParseResult(responseText);
    }

    private async Task<string> CallClaudeAsync(FoodVisionInput input, CancellationToken ct)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var contentItems = new List<object>();
        if (input.ImageBytes != null && input.MimeType != null)
        {
            contentItems.Add(new
            {
                type = "image",
                source = new
                {
                    type = "base64",
                    media_type = input.MimeType,
                    data = Convert.ToBase64String(input.ImageBytes)
                }
            });
        }
        contentItems.Add(new { type = "text", text = input.TextDescription ?? PromptText });

        var body = new
        {
            model = "claude-3-5-haiku-20241022",
            max_tokens = 1024,
            messages = new[]
            {
                new { role = "user", content = contentItems }
            },
            system = PromptText
        };

        var json = JsonSerializer.Serialize(body);
        _logger.LogDebug("FoodVisionAgent Claude prompt: {Prompt}", json);

        var response = await httpClient.PostAsync(
            "https://api.anthropic.com/v1/messages",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            ct);

        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "{}";
    }

    private async Task<string> CallOpenAiAsync(FoodVisionInput input, CancellationToken ct)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var contentItems = new List<object>();
        if (input.ImageBytes != null && input.MimeType != null)
        {
            var base64 = Convert.ToBase64String(input.ImageBytes);
            contentItems.Add(new
            {
                type = "image_url",
                image_url = new { url = $"data:{input.MimeType};base64,{base64}" }
            });
        }
        contentItems.Add(new { type = "text", text = input.TextDescription ?? PromptText });

        var messages = new List<object>
        {
            new { role = "system", content = PromptText },
            new { role = "user", content = (object)contentItems }
        };
        var body = new
        {
            model = "gpt-4o-mini",
            messages,
            max_tokens = 1024
        };

        var json = JsonSerializer.Serialize(body);
        _logger.LogDebug("FoodVisionAgent OpenAI prompt: {Prompt}", json);

        var response = await httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            ct);

        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";
    }

    private static FoodVisionResult ParseResult(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var ingredients = root.TryGetProperty("ingredients", out var ingProp)
            ? ingProp.EnumerateArray().Select(i => new FoodVisionIngredient(
                i.GetProperty("name").GetString() ?? "",
                i.GetProperty("amount").GetString() ?? "")).ToArray()
            : [];

        return new FoodVisionResult(
            ingredients,
            root.TryGetProperty("caloriesEstimate", out var cal) ? cal.GetDecimal() : 0,
            root.TryGetProperty("proteinG", out var pro) ? pro.GetDecimal() : 0,
            root.TryGetProperty("fatG", out var fat) ? fat.GetDecimal() : 0,
            root.TryGetProperty("carbsG", out var carb) ? carb.GetDecimal() : 0,
            root.TryGetProperty("portionEstimate", out var port) ? port.GetString() ?? "" : "",
            root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0
        );
    }
}
