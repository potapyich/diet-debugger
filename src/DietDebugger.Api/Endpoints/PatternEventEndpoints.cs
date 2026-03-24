using System.Security.Claims;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Api.Endpoints;

public static class PatternEventEndpoints
{
    public static IEndpointRouteBuilder MapPatternEventEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/pattern-events", ListPatternEvents).RequireAuthorization();
        app.MapPost("/pattern-events/{id:guid}/acknowledge", AcknowledgeEvent).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> ListPatternEvents(
        int? limit,
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var maxItems = Math.Min(limit ?? 20, 100);

        var events = await db.PatternEvents
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Acknowledged)
            .ThenByDescending(e => e.TriggeredAt)
            .Take(maxItems)
            .Select(e => new
            {
                e.Id, e.UserId, e.PatternKey,
                Type = e.Type.ToString(),
                e.TriggeredAt, e.Acknowledged, e.Message
            })
            .ToListAsync();

        return Results.Ok(events);
    }

    private static async Task<IResult> AcknowledgeEvent(
        Guid id,
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ev = await db.PatternEvents.FindAsync(id);
        if (ev == null || ev.UserId != userId) return Results.NotFound();

        ev.Acknowledged = true;
        await db.SaveChangesAsync();
        return Results.Ok();
    }
}
