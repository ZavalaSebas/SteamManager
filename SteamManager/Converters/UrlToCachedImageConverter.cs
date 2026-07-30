using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using SteamManager.Services;

namespace SteamManager.Converters;

public class UrlToCachedImageConverter : IValueConverter
{
    private static IImageCacheService? _cacheService;
    private static readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();

    public static void SetCacheService(IImageCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrEmpty(url))
            return null;

        if (_imageCache.TryGetValue(url, out var cached))
            return cached;

        if (_cacheService == null)
            return null;

        try
        {
            var image = _cacheService.GetFromCache(url);
            if (image != null)
            {
                _imageCache[url] = image;
                return image;
            }

            _ = LoadImageAsync(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Image cache lookup failed: {ex.Message}");
        }

        return null;
    }

    private async Task LoadImageAsync(string url)
    {
        try
        {
            if (_cacheService == null || _imageCache.ContainsKey(url))
                return;

            var image = await _cacheService.GetOrDownloadAsync(url);
            if (image != null)
            {
                _imageCache[url] = image;
                OnImageLoaded(url, image);
            }
        }
        catch
        {
            // Suppress exceptions in fire-and-forget
        }
    }

    private static void OnImageLoaded(string url, BitmapImage image)
    {
        ImageLoaded?.Invoke(null, new ImageLoadedEventArgs(url, image));
    }

    public static event EventHandler<ImageLoadedEventArgs>? ImageLoaded;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ImageLoadedEventArgs : EventArgs
{
    public string Url { get; }
    public BitmapImage Image { get; }

    public ImageLoadedEventArgs(string url, BitmapImage image)
    {
        Url = url;
        Image = image;
    }
}
