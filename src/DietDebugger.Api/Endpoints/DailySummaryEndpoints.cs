using System.Security.Claims;
using DietDebugger.Application;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Api.Endpoints;

public static class DailySummaryEndpoints
{
    public static IEndpointRouteBuilder MapDailySummaryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/daily-summary", GetDailySummary).RequireAuthorization();
        app.MapPost("/daily-summary/invalidate", InvalidateDailySummary).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> GetDailySummary(
        string? date,
        DailySummaryService summaryService,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!DateOnly.TryParse(date, out var parsedDate))
            return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });

        var summary = await summaryService.GetOrGenerateAsync(userId, parsedDate, null);

        return Results.Ok(new
        {
            summary.Id,
            summary.UserId,
            Date = summary.Date.ToString("yyyy-MM-dd"),
            summary.TotalCalories,
            summary.TotalProteinG,
            summary.TotalFatG,
            summary.TotalCarbsG,
            summary.Insights,
            summary.GeneratedAt,
            summary.IsStale
        });
    }

    private static async Task<IResult> InvalidateDailySummary(
        string? date,
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!DateOnly.TryParse(date, out var parsedDate))
            return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });

        var summary = await db.DailySummaries
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Date == parsedDate);

        if (summary != null)
        {
            summary.IsStale = true;
            await db.SaveChangesAsync();
        }

        return Results.Ok();
    }
}
