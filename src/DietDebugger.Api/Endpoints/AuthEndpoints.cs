using DietDebugger.Api.Validation;
using DietDebugger.Application.Auth;
using DietDebugger.Application.Auth.Commands;
using FluentValidation;

namespace DietDebugger.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", Register);

        group.MapPost("/login", async (LoginCommand cmd, AuthService authService, CancellationToken ct) =>
        {
            try
            {
                var result = await authService.LoginAsync(cmd, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException)
            {
                return Results.Unauthorized();
            }
        });

        group.MapPost("/refresh", async (RefreshCommand cmd, AuthService authService, CancellationToken ct) =>
        {
            try
            {
                var result = await authService.RefreshAsync(cmd, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException)
            {
                return Results.Unauthorized();
            }
        });

        return app;
    }

    private static async Task<IResult> Register(
        RegisterCommand cmd,
        IValidator<RegisterCommand> validator,
        AuthService authService,
        CancellationToken ct)
    {
        var validationError = await validator.ValidateRequestAsync(cmd);
        if (validationError != null) return validationError;

        try
        {
            var result = await authService.RegisterAsync(cmd, ct);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
