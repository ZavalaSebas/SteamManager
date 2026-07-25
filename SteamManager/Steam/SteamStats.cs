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
        value = 0;
        return SteamNative.SteamAPI_ISteamUserStats_GetStat(
            _client.GetUserStatsPointer(), name, ref value);
    }

    /// <summary>
    /// Gets a float stat value.
    /// </summary>
    public bool GetStat(string name, out float value)
    {
        value = 0f;
        return SteamNative.SteamAPI_ISteamUserStats_GetStat(
            _client.GetUserStatsPointer(), name, ref value);
    }

    /// <summary>
    /// Sets an integer stat value.
    /// </summary>
    public bool SetStat(string name, int value)
    {
        return SteamNative.SteamAPI_ISteamUserStats_SetStat(
            _client.GetUserStatsPointer(), name, value);
    }

    /// <summary>
    /// Sets a float stat value.
    /// </summary>
    public bool SetStat(string name, float value)
    {
        return SteamNative.SteamAPI_ISteamUserStats_SetStat(
            _client.GetUserStatsPointer(), name, value);
    }

    /// <summary>
    /// Persists all pending stat changes to Steam servers.
    /// </summary>
    public bool StoreStats()
    {
        return SteamNative.SteamAPI_ISteamUserStats_StoreStats(
            _client.GetUserStatsPointer());
    }

    /// <summary>
    /// Resets all stats. If achievementsToo is true, also resets achievements.
    /// </summary>
    public bool ResetAllStats(bool achievementsToo)
    {
        return SteamNative.SteamAPI_ISteamUserStats_ResetAllStats(
            _client.GetUserStatsPointer(), achievementsToo);
    }
}
