using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SteamManager.Steam;

public class SteamIcons
{
    private readonly SteamClient _client;

    public SteamIcons(SteamClient client)
    {
        _client = client;
    }

    public bool GetImageSize(int imageHandle, out int width, out int height)
    {
        return _client.Utils.GetImageSize(imageHandle, out width, out height);
    }

    public byte[]? GetImageRGBA(int imageHandle)
    {
        if (imageHandle == 0)
            return null;

        if (!GetImageSize(imageHandle, out int width, out int height))
            return null;

        int bufferSize = width * height * 4;
        byte[] buffer = new byte[bufferSize];

        if (!_client.Utils.GetImageRGBA(imageHandle, buffer, bufferSize))
            return null;

        return buffer;
    }

    public BitmapSource? GetBitmapSource(int imageHandle)
    {
        var rgba = GetImageRGBA(imageHandle);
        if (rgba == null)
            return null;

        if (!GetImageSize(imageHandle, out int width, out int height))
            return null;

        var bitmap = BitmapSource.Create(
            width, height,
            96, 96,
            PixelFormats.Bgra32,
            null,
            rgba, width * 4);

        bitmap.Freeze();
        return bitmap;
    }
}