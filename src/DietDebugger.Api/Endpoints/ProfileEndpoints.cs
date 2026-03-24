using System.Security.Claims;
using DietDebugger.Api.Validation;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using FluentValidation;

namespace DietDebugger.Api.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/profile", GetProfile).RequireAuthorization();
        app.MapPut("/profile", UpdateProfile).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> GetProfile(AppDbContext db, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await db.Users.FindAsync(userId);
        if (u == null) return Results.NotFound();
        return Results.Ok(ToDto(u));
    }

    private static async Task<IResult> UpdateProfile(
        ProfileRequest req,
        IValidator<ProfileRequest> validator,
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var validationError = await validator.ValidateRequestAsync(req);
        if (validationError != null) return validationError;

        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await db.Users.FindAsync(userId);
        if (u == null) return Results.NotFound();

        u.WeightKg = req.WeightKg ?? u.WeightKg;
        u.HeightCm = req.HeightCm ?? u.HeightCm;
        u.Age = req.Age ?? u.Age;
        u.Sex = req.Sex ?? u.Sex;
        u.DietType = req.DietType ?? u.DietType;
        u.FeedbackTone = req.FeedbackTone ?? u.FeedbackTone;
        u.PreferredLanguage = req.PreferredLanguage ?? u.PreferredLanguage;

        // Set ProfileCompletedAt when all required fields are filled
        if (u.WeightKg.HasValue && u.HeightCm.HasValue && u.Age.HasValue && u.Sex.HasValue)
        {
            u.ProfileCompletedAt ??= DateTime.UtcNow;
        }
        else
        {
            u.ProfileCompletedAt = null;
        }

        await db.SaveChangesAsync();
        return Results.Ok(ToDto(u));
    }

    private static object ToDto(User u) => new
    {
        u.Id, u.Email,
        u.WeightKg, u.HeightCm, u.Age,
        Sex = u.Sex?.ToString(),
        DietType = u.DietType?.ToString(),
        FeedbackTone = u.FeedbackTone.ToString(),
        u.PreferredLanguage,
        u.ProfileCompletedAt
    };
}

public record ProfileRequest(
    decimal? WeightKg,
    int? HeightCm,
    int? Age,
    Sex? Sex,
    DietType? DietType,
    FeedbackTone? FeedbackTone,
    string? PreferredLanguage);
