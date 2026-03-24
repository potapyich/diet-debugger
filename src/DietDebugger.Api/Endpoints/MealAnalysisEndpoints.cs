using System.Security.Claims;
using System.Text.Json;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.AI;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DietDebugger.Api.Endpoints;

public static class MealAnalysisEndpoints
{
    public static IEndpointRouteBuilder MapMealAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/meals/analyze", AnalyzeMeal).RequireAuthorization();
        app.MapGet("/meals/analyze/{jobId:guid}", GetJobStatus).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> AnalyzeMeal(
        HttpRequest request,
        AppDbContext db,
        AnalysisJobChannel channel,
        IFileStorage fileStorage,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        FoodVisionInput input;
        string? fileKey = null;

        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("image");
            if (file == null)
                return Results.BadRequest(new { error = "image file required" });

            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest(new { error = "Image must be under 10 MB." });

            if (!file.ContentType.StartsWith("image/"))
                return Results.BadRequest(new { error = "File must be an image." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var imageBytes = ms.ToArray();

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            fileKey = await fileStorage.SaveTemporaryAsync(imageBytes, ext, TimeSpan.FromHours(1));

            input = new FoodVisionInput
            {
                ImageBytes = imageBytes,
                MimeType = file.ContentType
            };
        }
        else
        {
            var body = await request.ReadFromJsonAsync<TextAnalysisRequest>();
            if (body?.Text == null)
                return Results.BadRequest(new { error = "text required" });
            input = new FoodVisionInput { TextDescription = body.Text };
        }

        var job = new AnalysisJob { UserId = userId };
        db.AnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        await channel.Writer.WriteAsync(new AnalysisJobRequest(job.Id, input, fileKey));

        return Results.Ok(new { jobId = job.Id });
    }

    private static async Task<IResult> GetJobStatus(
        Guid jobId,
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var job = await db.AnalysisJobs.FindAsync(jobId);
        if (job == null || job.UserId != userId)
            return Results.NotFound();

        var statusString = job.Status switch
        {
            AnalysisJobStatus.Pending => "pending",
            AnalysisJobStatus.Ready => "ready",
            AnalysisJobStatus.Failed => "failed",
            _ => "pending"
        };

        if (job.Status == AnalysisJobStatus.Ready && job.Result != null)
        {
            var result = JsonSerializer.Deserialize<object>(job.Result);
            return Results.Ok(new { status = statusString, result });
        }

        if (job.Status == AnalysisJobStatus.Failed)
            return Results.Ok(new { status = statusString, error = job.Error });

        return Results.Ok(new { status = statusString });
    }

    private record TextAnalysisRequest(string Text);
}
