using DietDebugger.Api.Endpoints;
using DietDebugger.Application.Auth.Commands;
using FluentValidation;

namespace DietDebugger.Api.Validation;

public static class ValidationExtensions
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<RegisterCommand>, RegisterCommandValidator>();
        services.AddScoped<IValidator<ProfileRequest>, ProfileRequestValidator>();
        services.AddScoped<IValidator<GoalRequest>, GoalRequestValidator>();
        services.AddScoped<IValidator<HabitRequest>, HabitRequestValidator>();
        return services;
    }

    public static async Task<IResult?> ValidateRequestAsync<T>(
        this IValidator<T> validator, T instance)
    {
        var result = await validator.ValidateAsync(instance);
        if (result.IsValid) return null;

        var errors = result.Errors
            .GroupBy(e => char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return Results.BadRequest(new { errors });
    }
}
