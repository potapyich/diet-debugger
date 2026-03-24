namespace DietDebugger.Application.Interfaces;

public interface IFileStorage
{
    /// <summary>Returns an opaque key identifying the stored file.</summary>
    Task<string> SaveTemporaryAsync(byte[] data, string extension, TimeSpan ttl, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);
}
