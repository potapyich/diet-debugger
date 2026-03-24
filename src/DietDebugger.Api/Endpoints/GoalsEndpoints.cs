using System.Security.Claims;
using DietDebugger.Api.Validation;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Api.Endpoints;

public static class GoalsEndpoints
{
    public static IEndpointRouteBuilder MapGoalsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/goals", GetGoals).RequireAuthorization();
        app.MapPut("/goals", UpsertGoals).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> GetGoals(AppDbContext db, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var goal = await db.UserGoals.FirstOrDefaultAsync(g => g.UserId == userId);
        if (goal == null) return Results.NotFound();
        return Results.Ok(ToDto(goal));
    }

    private static async Task<IResult> UpsertGoals(
        GoalRequest req,
        IValidator<GoalRequest> validator,
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var validationError = await validator.ValidateRequestAsync(req);
        if (validationError != null) return validationError;

        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var goal = await db.UserGoals.FirstOrDefaultAsync(g => g.UserId == userId);

        if (goal == null)
        {
            goal = new UserGoal { UserId = userId };
            db.UserGoals.Add(goal);
        }

        goal.GoalType = req.GoalType;
        goal.DailyCalorieTarget = req.DailyCalorieTarget;
        goal.ProteinTargetG = req.ProteinTargetG;
        goal.FatTargetG = req.FatTargetG;
        goal.CarbsTargetG = req.CarbsTargetG;
        goal.ActiveSince = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(ToDto(goal));
    }

    private static object ToDto(UserGoal g) => new
    {
        g.Id, g.UserId,
        GoalType = g.GoalType.ToString(),
        g.DailyCalorieTarget,
        g.ProteinTargetG, g.FatTargetG, g.CarbsTargetG,
        g.ActiveSince
    };
}

public record GoalRequest(
    GoalType GoalType,
    int DailyCalorieTarget,
    int? ProteinTargetG,
    int? FatTargetG,
    int? CarbsTargetG);
