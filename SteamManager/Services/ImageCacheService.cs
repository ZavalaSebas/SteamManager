using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace SteamManager.Services;

public class ImageCacheService : IImageCacheService
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromDays(7);
    // Grace period for disk cleanup: keep expired files longer so that
    // stale-while-revalidate in GetOrDownloadAsync can still use them
    // as fallback when the download fails. Deleting at TTL would leave
    // the UI grey (placeholder) whenever re-download fails.
    private readonly TimeSpan _cleanupGrace = TimeSpan.FromDays(30);
    private readonly Dictionary<string, BitmapImage> _memoryCache = [];
    private readonly object _lock = new();

    public ImageCacheService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", Config.UserAgent);
        _httpClient.Timeout = TimeSpan.FromSeconds(Config.RequestTimeoutSeconds);

        _cacheDir = Config.ImageCachePath;
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<BitmapImage?> GetOrDownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        lock (_lock)
        {
            if (_memoryCache.TryGetValue(url, out var cached))
                return cached;
        }

        string filePath = GetCacheFilePath(url);

        if (File.Exists(filePath))
        {
            var fileInfo = new FileInfo(filePath);
            if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc < _cacheTtl)
            {
                var image = LoadImage(filePath);
                if (image != null)
                {
                    lock (_lock) _memoryCache[url] = image;
                    return image;
                }
            }
        }

        try
        {
            var imageBytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
            if (imageBytes.Length == 0) throw new InvalidDataException("Empty image bytes");
            await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
            var image = LoadImage(filePath);
            if (image != null) { lock (_lock) _memoryCache[url] = image; }
            return image;
        }
        catch
        {
            // stale-while-revalidate: if download fails, return expired file as fallback
            if (File.Exists(filePath))
            {
                var stale = LoadImage(filePath);
                if (stale != null) { lock (_lock) _memoryCache[url] = stale; return stale; }
            }
            return null;
        }
    }

    public BitmapImage? GetFromCache(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        lock (_lock)
        {
            if (_memoryCache.TryGetValue(url, out var cached))
                return cached;
        }

        string filePath = GetCacheFilePath(url);
        if (!File.Exists(filePath))
            return null;

        var image = LoadImage(filePath);
        if (image != null)
        {
            lock (_lock)
            {
                _memoryCache[url] = image;
            }
        }
        return image;
    }

    public void CleanupExpired()
    {
        try
        {
            var cutoff = DateTime.UtcNow - _cleanupGrace;
            foreach (var file in Directory.EnumerateFiles(_cacheDir, "*", SearchOption.AllDirectories))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTimeUtc < cutoff) fileInfo.Delete();
            }
        }
        catch { }
    }

    public void ClearAll()
    {
        lock (_lock) _memoryCache.Clear();
        try { Converters.UrlToCachedImageConverter.ClearCache(); } catch { }
        try
        {
            foreach (var file in Directory.EnumerateFiles(_cacheDir, "*", SearchOption.AllDirectories))
                File.Delete(file);
        }
        catch { }
    }

    private string GetCacheFilePath(string url)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(url));
        string hashStr = Convert.ToHexString(hash).ToLowerInvariant();
        string extension = GetExtension(url);
        return Path.Combine(_cacheDir, $"{hashStr}{extension}");
    }

    private static string GetExtension(string url)
    {
        if (url.Contains(".png", StringComparison.OrdinalIgnoreCase))
            return ".png";
        if (url.Contains(".jpg", StringComparison.OrdinalIgnoreCase) || url.Contains(".jpeg", StringComparison.OrdinalIgnoreCase))
            return ".jpg";
        if (url.Contains(".gif", StringComparison.OrdinalIgnoreCase))
            return ".gif";
        if (url.Contains(".webp", StringComparison.OrdinalIgnoreCase))
            return ".webp";
        return ".jpg";
    }

    private static BitmapImage? LoadImage(string filePath)
    {
        try
        {
            var bytes = File.ReadAllBytes(filePath);
            if (bytes.Length == 0) return null;
            var image = new BitmapImage();
            using var ms = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            // corrupted file — delete so next try re-downloads
            try { File.Delete(filePath); } catch { }
            return null;
        }
    }
}
