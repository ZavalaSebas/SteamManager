namespace SteamManager.Steam;

/// <summary>
/// Represents the active Steam session for a specific AppID.
/// Groups all Steam API components together.
/// </summary>
public class SteamContext : IDisposable
{
    private readonly SteamClient _client;

    public SteamClient Client => _client;
    public SteamAchievements Achievements { get; }
    public SteamStats Stats { get; }
    public SteamApps Apps { get; }

    public bool IsInitialized => _client.IsInitialized;
    public uint AppId => _client.CurrentAppId;
    public ulong SteamId => _client.IsInitialized ? _client.User.GetSteamId() : 0;

    public SteamContext()
    {
        _client = new SteamClient();
        Achievements = new SteamAchievements(_client);
        Stats = new SteamStats(_client);
        Apps = new SteamApps(_client);
    }

    /// <summary>
    /// Initializes the Steam session for the specified app.
    /// </summary>
    public bool Initialize(uint appId)
    {
        return _client.Init(appId);
    }

    /// <summary>
    /// Requests current stats from Steam servers.
    /// Wait for UserStatsReceived_t callback after calling this.
    /// </summary>
    public bool RequestStats()
    {
        return _client.RequestCurrentStats();
    }

    /// <summary>
    /// Runs pending callbacks.
    /// </summary>
    public void RunCallbacks()
    {
        _client.RunCallbacks();
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
