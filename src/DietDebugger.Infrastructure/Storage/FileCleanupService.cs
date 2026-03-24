using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DietDebugger.Infrastructure.Storage;

public class FileCleanupService : BackgroundService
{
    private readonly LocalFileStorage _storage;
    private readonly ILogger<FileCleanupService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public FileCleanupService(LocalFileStorage storage, ILogger<FileCleanupService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _storage.CleanupExpired();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File cleanup failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
