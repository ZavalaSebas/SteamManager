namespace SteamManager.Steam;

/// <summary>
/// Provides access to Steam achievement icon images.
/// Handles decoding RGBA pixel data from the Steam API.
/// </summary>
public class SteamIcons
{
    private readonly SteamClient _client;

    public SteamIcons(SteamClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Gets the size of an image by its handle.
    /// </summary>
    public bool GetImageSize(int imageHandle, out int width, out int height)
    {
        return _client.Utils.GetImageSize(imageHandle, out width, out height);
    }

    /// <summary>
    /// Gets the RGBA pixel data for an image by its handle.
    /// </summary>
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
}