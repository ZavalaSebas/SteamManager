using SteamManager.Models;

namespace SteamManager.Steam;

/// <summary>
/// Provides read/write access to Steam stats for the current app.
/// </summary>
public class SteamStats
{
    private readonly SteamClient _client;

    public SteamStats(SteamClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Gets an integer stat value.
    /// </summary>
    public bool GetStat(string name, out int value)
    {
        return _client.UserStats.GetStat(name, out value);
    }

    /// <summary>
    /// Gets a float stat value.
    /// </summary>
    public bool GetStat(string name, out float value)
    {
        return _client.UserStats.GetStat(name, out value);
    }

    /// <summary>
    /// Sets an integer stat value.
    /// </summary>
    public bool SetStat(string name, int value)
    {
        return _client.UserStats.SetStat(name, value);
    }

    /// <summary>
    /// Sets a float stat value.
    /// </summary>
    public bool SetStat(string name, float value)
    {
        return _client.UserStats.SetStat(name, value);
    }

    /// <summary>
    /// Persists all pending stat changes to Steam servers.
    /// </summary>
    public bool StoreStats()
    {
        return _client.UserStats.StoreStats();
    }

    /// <summary>
    /// Resets all stats. If achievementsToo is true, also resets achievements.
    /// </summary>
    public bool ResetAllStats(bool achievementsToo)
    {
        return _client.UserStats.ResetAllStats(achievementsToo);
    }
}
