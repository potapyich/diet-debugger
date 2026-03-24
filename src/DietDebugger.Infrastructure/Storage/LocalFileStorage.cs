using DietDebugger.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DietDebugger.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(string basePath, ILogger<LocalFileStorage> logger)
    {
        _basePath = basePath;
        _logger = logger;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveTemporaryAsync(
        byte[] data,
        string extension,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_basePath, fileName);
        await File.WriteAllBytesAsync(filePath, data, ct);

        // Store expiry time in a sidecar metadata file
        var expiry = DateTime.UtcNow.Add(ttl);
        var metaPath = filePath + ".meta";
        await File.WriteAllTextAsync(metaPath, expiry.ToString("O"), ct);

        _logger.LogDebug("Saved temporary file {Key}, expires {Expiry}", fileName, expiry);
        return fileName;
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_basePath, key);
        TryDelete(filePath);
        TryDelete(filePath + ".meta");
        return Task.CompletedTask;
    }

    public void CleanupExpired()
    {
        if (!Directory.Exists(_basePath)) return;

        foreach (var metaFile in Directory.GetFiles(_basePath, "*.meta"))
        {
            try
            {
                var expiryText = File.ReadAllText(metaFile);
                if (!DateTime.TryParseExact(expiryText, "O", null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var expiry)) continue;
                if (DateTime.UtcNow < expiry) continue;

                var dataFile = metaFile[..^5]; // strip ".meta"
                TryDelete(dataFile);
                TryDelete(metaFile);
                _logger.LogDebug("Deleted expired file {File}", dataFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process meta file {Meta}", metaFile);
            }
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort */ }
    }
}
