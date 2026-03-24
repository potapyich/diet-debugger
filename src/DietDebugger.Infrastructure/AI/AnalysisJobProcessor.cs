using System.Text.Json;
using System.Threading.Channels;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DietDebugger.Infrastructure.AI;

public record AnalysisJobRequest(Guid JobId, FoodVisionInput Input, string? FileKey = null);

public class AnalysisJobChannel
{
    private readonly Channel<AnalysisJobRequest> _channel =
        Channel.CreateUnbounded<AnalysisJobRequest>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<AnalysisJobRequest> Writer => _channel.Writer;
    public ChannelReader<AnalysisJobRequest> Reader => _channel.Reader;
}

public class AnalysisJobProcessor : BackgroundService
{
    private readonly AnalysisJobChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFoodVisionAgent _agent;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<AnalysisJobProcessor> _logger;

    public AnalysisJobProcessor(
        AnalysisJobChannel channel,
        IServiceScopeFactory scopeFactory,
        IFoodVisionAgent agent,
        IFileStorage fileStorage,
        ILogger<AnalysisJobProcessor> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _agent = agent;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessJobAsync(request, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(AnalysisJobRequest request, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var job = await db.AnalysisJobs.FindAsync([request.JobId], ct);
        if (job == null) return;

        try
        {
            var result = await _agent.AnalyzeAsync(request.Input, ct);
            job.Status = AnalysisJobStatus.Ready;
            job.Result = JsonSerializer.Serialize(result);
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", request.JobId);
            job.Status = AnalysisJobStatus.Failed;
            job.Error = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
        }
        finally
        {
            // Delete the temporary photo after the job finishes
            if (request.FileKey != null)
                await _fileStorage.DeleteAsync(request.FileKey, ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
