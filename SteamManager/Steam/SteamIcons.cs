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
    public bool GetImageSize(int imageHandle, out uint width, out uint height)
    {
        return SteamNative.SteamAPI_ISteamUtils_GetImageSize(
            _client.GetUtilsPointer(), imageHandle, out width, out height);
    }

    /// <summary>
    /// Gets the RGBA pixel data for an image by its handle.
    /// </summary>
    public byte[]? GetImageRGBA(int imageHandle)
    {
        if (imageHandle == 0)
            return null;

        if (!GetImageSize(imageHandle, out uint width, out uint height))
            return null;

        int bufferSize = (int)(width * height * 4);
        byte[] buffer = new byte[bufferSize];

        if (!SteamNative.SteamAPI_ISteamUtils_GetImageRGBA(
            _client.GetUtilsPointer(), imageHandle, buffer, bufferSize))
            return null;

        return buffer;
    }
}
