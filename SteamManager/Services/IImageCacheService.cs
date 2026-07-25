using System.Windows.Media.Imaging;

namespace SteamManager.Services;

/// <summary>
/// Interface for caching downloaded images locally.
/// </summary>
public interface IImageCacheService
{
    /// <summary>
    /// Gets an image from cache or downloads it if not cached/expired.
    /// </summary>
    Task<BitmapImage?> GetOrDownloadAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an image from cache synchronously (must be in cache already).
    /// </summary>
    BitmapImage? GetFromCache(string url);

    /// <summary>
    /// Clears all cached images older than the configured TTL.
    /// </summary>
    void CleanupExpired();

    /// <summary>
    /// Clears all cached images.
    /// </summary>
    void ClearAll();
}
