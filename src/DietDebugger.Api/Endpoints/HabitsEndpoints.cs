using System.Security.Claims;
using DietDebugger.Api.Validation;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Api.Endpoints;

public static class HabitsEndpoints
{
    private const int MaxHabitsPerUser = 10;

    public static IEndpointRouteBuilder MapHabitsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/habits", CreateHabit).RequireAuthorization();
        app.MapGet("/habits", GetHabits).RequireAuthorization();
        app.MapDelete("/habits/{id:guid}", DeleteHabit).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> CreateHabit(
        HabitRequest req,
        IValidator<HabitRequest> validator,
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var validationError = await validator.ValidateRequestAsync(req);
        if (validationError != null) return validationError;

        var userId = GetUserId(user);
        var count = await db.UserHabits.CountAsync(h => h.UserId == userId);
        if (count >= MaxHabitsPerUser)
            return Results.BadRequest(new { error = $"Maximum {MaxHabitsPerUser} habits allowed." });

        var habit = new UserHabit
        {
            UserId = userId,
            HabitDescription = req.HabitDescription.Trim()
        };
        db.UserHabits.Add(habit);
        await db.SaveChangesAsync();

        return Results.Created($"/habits/{habit.Id}", ToDto(habit));
    }

    private static async Task<IResult> GetHabits(AppDbContext db, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var habits = await db.UserHabits
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();
        return Results.Ok(habits.Select(ToDto));
    }

    private static async Task<IResult> DeleteHabit(Guid id, AppDbContext db, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var habit = await db.UserHabits.FindAsync(id);
        if (habit == null || habit.UserId != userId) return Results.NotFound();

        db.UserHabits.Remove(habit);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static object ToDto(UserHabit h) => new { h.Id, h.HabitDescription, h.CreatedAt };
}

public record HabitRequest(string HabitDescription);
