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
            if (DateTime.Now - fileInfo.LastWriteTime < _cacheTtl)
            {
                var image = LoadImage(filePath);
                if (image != null)
                {
                    lock (_lock)
                    {
                        _memoryCache[url] = image;
                    }
                    return image;
                }
            }
        }

        try
        {
            var imageBytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
            await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);

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
        catch
        {
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
            var cutoff = DateTime.Now - _cacheTtl;
            foreach (var file in Directory.EnumerateFiles(_cacheDir, "*", SearchOption.AllDirectories))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime < cutoff)
                {
                    fileInfo.Delete();
                }
            }
        }
        catch { }
    }

    public void ClearAll()
    {
        lock (_lock)
        {
            _memoryCache.Clear();
        }

        try
        {
            foreach (var file in Directory.EnumerateFiles(_cacheDir, "*", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }
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
            return null;
        }
    }
}
