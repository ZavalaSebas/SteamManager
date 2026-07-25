using SteamManager.Models;

namespace SteamManager.Steam;

/// <summary>
/// Provides access to the user's Steam game library.
/// </summary>
public class SteamApps
{
    private readonly SteamClient _client;

    public SteamApps(SteamClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Checks if the user owns a specific app.
    /// </summary>
    public bool IsSubscribedApp(uint appId)
    {
        return _client.Apps.IsSubscribedApp(appId);
    }
}
