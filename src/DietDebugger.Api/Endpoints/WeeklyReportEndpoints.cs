using System.Security.Claims;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Api.Endpoints;

public static class WeeklyReportEndpoints
{
    public static IEndpointRouteBuilder MapWeeklyReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/weekly-report", GetWeeklyReport).RequireAuthorization();
        app.MapPost("/weekly-report/invalidate", InvalidateWeeklyReport).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> GetWeeklyReport(
        string? weekStart,
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!DateOnly.TryParse(weekStart, out var parsedDate))
            return Results.BadRequest(new { error = "Invalid date. Use YYYY-MM-DD (Monday)." });

        if (parsedDate.DayOfWeek != DayOfWeek.Monday)
            return Results.BadRequest(new { error = "weekStart must be a Monday." });

        var report = await db.WeeklyReports
            .FirstOrDefaultAsync(r => r.UserId == userId && r.WeekStartDate == parsedDate);

        if (report == null)
            return Results.NotFound();

        return Results.Ok(new
        {
            report.Id, report.UserId,
            WeekStartDate = report.WeekStartDate.ToString("yyyy-MM-dd"),
            report.Patterns,
            report.CalendarDayStatuses,
            report.GeneratedAt,
            report.IsStale
        });
    }

    private static async Task<IResult> InvalidateWeeklyReport(
        string? weekStart,
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!DateOnly.TryParse(weekStart, out var parsedDate))
            return Results.BadRequest(new { error = "Invalid date." });

        var report = await db.WeeklyReports
            .FirstOrDefaultAsync(r => r.UserId == userId && r.WeekStartDate == parsedDate);

        if (report != null)
        {
            report.IsStale = true;
            await db.SaveChangesAsync();
        }

        return Results.Ok();
    }
}
