using System.Security.Claims;
using System.Text.Json;
using DietDebugger.Application;
using DietDebugger.Application.Interfaces;

namespace DietDebugger.Api.Endpoints;

public static class MonthlyReportEndpoints
{
    public static IEndpointRouteBuilder MapMonthlyReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/monthly-report", GetMonthlyReport).RequireAuthorization();
        app.MapGet("/forecast", GetForecast).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> GetMonthlyReport(
        string? month,
        MonthlyReportService reportService,
        ClaimsPrincipal user,
        HttpContext ctx)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        DateOnly monthStart;
        if (!string.IsNullOrWhiteSpace(month) && DateOnly.TryParseExact(month, "yyyy-MM", out var parsed))
            monthStart = new DateOnly(parsed.Year, parsed.Month, 1);
        else
            monthStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var lang = ctx.Request.Headers["Accept-Language"].FirstOrDefault();
        var report = await reportService.GetOrGenerateAsync(userId, monthStart, lang);

        if (report == null)
            return Results.NotFound(new { error = "No weekly data available for this month." });

        StrategyAssessment? assessment = null;
        try { assessment = JsonSerializer.Deserialize<StrategyAssessment>(report.Assessment,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { }

        return Results.Ok(new
        {
            report.Id,
            report.UserId,
            report.MonthStart,
            report.GeneratedAt,
            Assessment = assessment
        });
    }

    private static async Task<IResult> GetForecast(
        ForecastService forecastService,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var forecast = await forecastService.GetForecastAsync(userId);

        if (forecast == null)
            return Results.Ok(new { message = "Not enough data yet (need 7+ days)", forecast = (object?)null });

        return Results.Ok(new
        {
            forecast = new
            {
                averageDailyDeltaKcal = Math.Round(forecast.AverageDailyDeltaKcal, 1),
                deficitConsistencyPercent = Math.Round(forecast.DeficitConsistencyPercent, 1),
                estimatedWeightChangePer30Days = forecast.EstimatedWeightChangePer30Days
            }
        });
    }
}
