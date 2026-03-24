using DietDebugger.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace DietDebugger.Tests.Storage;

[Trait("Category", "FileStorage")]
public class FileStorageTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalFileStorage _storage;

    public FileStorageTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dd_test_{Guid.NewGuid()}");
        _storage = new LocalFileStorage(_tempDir, NullLogger<LocalFileStorage>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task SaveTemporary_CreatesFile()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var key = await _storage.SaveTemporaryAsync(data, ".jpg", TimeSpan.FromHours(1));

        var filePath = Path.Combine(_tempDir, key);
        Assert.True(File.Exists(filePath));
        Assert.Equal(data, await File.ReadAllBytesAsync(filePath));
    }

    [Fact]
    public async Task SaveTemporary_CreatesSidecarMetaFile()
    {
        var key = await _storage.SaveTemporaryAsync([1, 2, 3], ".jpg", TimeSpan.FromHours(1));
        var metaPath = Path.Combine(_tempDir, key + ".meta");
        Assert.True(File.Exists(metaPath));
    }

    [Fact]
    public async Task Delete_RemovesFileAndMeta()
    {
        var key = await _storage.SaveTemporaryAsync([1, 2, 3], ".jpg", TimeSpan.FromHours(1));
        await _storage.DeleteAsync(key);

        Assert.False(File.Exists(Path.Combine(_tempDir, key)));
        Assert.False(File.Exists(Path.Combine(_tempDir, key + ".meta")));
    }

    [Fact]
    public async Task CleanupExpired_DeletesExpiredFiles()
    {
        // Save with negative TTL (already expired)
        var key = await _storage.SaveTemporaryAsync([1, 2, 3], ".jpg", TimeSpan.FromSeconds(-1));

        _storage.CleanupExpired();

        Assert.False(File.Exists(Path.Combine(_tempDir, key)));
    }

    [Fact]
    public async Task CleanupExpired_KeepsNonExpiredFiles()
    {
        var key = await _storage.SaveTemporaryAsync([1, 2, 3], ".jpg", TimeSpan.FromHours(1));

        _storage.CleanupExpired();

        Assert.True(File.Exists(Path.Combine(_tempDir, key)));
    }

    [Fact]
    public async Task SaveTemporary_ReturnsOpaqueKey_NotFullPath()
    {
        var key = await _storage.SaveTemporaryAsync([1, 2, 3], ".jpg", TimeSpan.FromHours(1));

        // Key should be a filename, not a full path
        Assert.False(Path.IsPathRooted(key));
        Assert.DoesNotContain("/", key);
        Assert.DoesNotContain("\\", key);
    }
}
