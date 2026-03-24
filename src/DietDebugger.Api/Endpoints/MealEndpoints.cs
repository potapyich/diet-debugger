using System.Security.Claims;
using System.Text.Json;
using DietDebugger.Application;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DietDebugger.Api.Endpoints;

public static class MealEndpoints
{
    public static IEndpointRouteBuilder MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/meals", SaveMeal).RequireAuthorization();
        app.MapGet("/meals/{id:guid}", GetMeal).RequireAuthorization();
        app.MapPut("/meals/{id:guid}", UpdateMeal).RequireAuthorization();
        app.MapDelete("/meals/{id:guid}", DeleteMeal).RequireAuthorization();
        app.MapGet("/meals", ListMeals).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> SaveMeal(
        MealRequest req,
        AppDbContext db,
        PatternBreakerService patternBreaker,
        ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var loggedAt = req.LoggedAt ?? DateTime.UtcNow;
        var meal = new Meal
        {
            UserId = userId,
            LoggedAt = loggedAt,
            Calories = req.Calories,
            ProteinG = req.ProteinG,
            FatG = req.FatG,
            CarbsG = req.CarbsG,
            Ingredients = req.Ingredients ?? "[]",
            PortionEstimate = req.PortionEstimate ?? string.Empty,
            ConfidenceScore = req.ConfidenceScore,
            Source = req.Source,
            Notes = req.Notes
        };
        db.Meals.Add(meal);
        await db.SaveChangesAsync();

        // Run pattern detection without blocking the response
        _ = patternBreaker.RunAsync(userId, DateOnly.FromDateTime(loggedAt));

        return Results.Ok(meal.ToDto());
    }

    private static async Task<IResult> GetMeal(Guid id, AppDbContext db, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var meal = await db.Meals.FindAsync(id);
        if (meal == null) return Results.NotFound();
        if (meal.UserId != userId) return Results.Forbid();
        return Results.Ok(meal.ToDto());
    }

    private static async Task<IResult> UpdateMeal(
        Guid id,
        MealRequest req,
        AppDbContext db,
        DailySummaryService summaryService,
        ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var meal = await db.Meals.FindAsync(id);
        if (meal == null) return Results.NotFound();
        if (meal.UserId != userId) return Results.Forbid();

        meal.LoggedAt = req.LoggedAt ?? meal.LoggedAt;
        meal.Calories = req.Calories;
        meal.ProteinG = req.ProteinG;
        meal.FatG = req.FatG;
        meal.CarbsG = req.CarbsG;
        meal.Ingredients = req.Ingredients ?? meal.Ingredients;
        meal.PortionEstimate = req.PortionEstimate ?? meal.PortionEstimate;
        meal.ConfidenceScore = req.ConfidenceScore;
        meal.Source = req.Source;
        meal.Notes = req.Notes;

        await db.SaveChangesAsync();
        await summaryService.InvalidateAsync(userId, DateOnly.FromDateTime(meal.LoggedAt));
        return Results.Ok(meal.ToDto());
    }

    private static async Task<IResult> DeleteMeal(
        Guid id,
        AppDbContext db,
        DailySummaryService summaryService,
        ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var meal = await db.Meals.FindAsync(id);
        if (meal == null) return Results.NotFound();
        if (meal.UserId != userId) return Results.Forbid();

        var mealDate = DateOnly.FromDateTime(meal.LoggedAt);
        db.Meals.Remove(meal);
        await db.SaveChangesAsync();
        await summaryService.InvalidateAsync(userId, mealDate);
        return Results.NoContent();
    }

    private static async Task<IResult> ListMeals(
        string? date,
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        IQueryable<Meal> query = db.Meals.Where(m => m.UserId == userId);

        if (DateOnly.TryParse(date, out var parsedDate))
        {
            var start = parsedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = start.AddDays(1);
            query = query.Where(m => m.LoggedAt >= start && m.LoggedAt < end);
        }

        var meals = await query.OrderBy(m => m.LoggedAt).ToListAsync();
        return Results.Ok(meals.Select(m => m.ToDto()));
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public record MealRequest(
    DateTime? LoggedAt,
    decimal Calories,
    decimal ProteinG,
    decimal FatG,
    decimal CarbsG,
    string? Ingredients,
    string? PortionEstimate,
    decimal ConfidenceScore,
    MealSource Source,
    string? Notes);

public static class MealExtensions
{
    public static object ToDto(this Meal m) => new
    {
        m.Id, m.UserId, m.LoggedAt, m.Calories, m.ProteinG, m.FatG, m.CarbsG,
        m.Ingredients, m.PortionEstimate, m.ConfidenceScore, Source = m.Source.ToString(), m.Notes
    };
}
