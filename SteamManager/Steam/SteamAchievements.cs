using SteamManager.Models;

namespace SteamManager.Steam;

/// <summary>
/// Provides read/write access to Steam achievements for the current app.
/// </summary>
public class SteamAchievements
{
    private readonly SteamClient _client;

    public SteamAchievements(SteamClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Gets the total number of achievements for the current app.
    /// </summary>
    public uint GetAchievementCount()
    {
        return SteamNative.SteamAPI_ISteamUserStats_GetNumAchievements(
            _client.GetUserStatsPointer());
    }

    /// <summary>
    /// Gets the API name of the achievement at the specified index.
    /// </summary>
    public string GetAchievementName(uint index)
    {
        IntPtr namePtr = SteamNative.SteamAPI_ISteamUserStats_GetAchievementName(
            _client.GetUserStatsPointer(), index);
        return namePtr != IntPtr.Zero
            ? System.Runtime.InteropServices.Marshal.PtrToStringAnsi(namePtr) ?? string.Empty
            : string.Empty;
    }

    /// <summary>
    /// Gets whether a specific achievement is unlocked.
    /// </summary>
    public bool GetAchievement(string name, out bool isUnlocked)
    {
        return SteamNative.SteamAPI_ISteamUserStats_GetAchievement(
            _client.GetUserStatsPointer(), name, out isUnlocked);
    }

    /// <summary>
    /// Sets (unlocks) a specific achievement.
    /// </summary>
    public bool SetAchievement(string name)
    {
        return SteamNative.SteamAPI_ISteamUserStats_SetAchievement(
            _client.GetUserStatsPointer(), name);
    }

    /// <summary>
    /// Clears (locks) a specific achievement.
    /// </summary>
    public bool ClearAchievement(string name)
    {
        return SteamNative.SteamAPI_ISteamUserStats_ClearAchievement(
            _client.GetUserStatsPointer(), name);
    }

    /// <summary>
    /// Gets the unlock state and time for a specific achievement.
    /// </summary>
    public bool GetAchievementAndUnlockTime(string name, out bool isUnlocked, out uint unlockTime)
    {
        return SteamNative.SteamAPI_ISteamUserStats_GetAchievementAndUnlockTime(
            _client.GetUserStatsPointer(), name, out isUnlocked, out unlockTime);
    }

    /// <summary>
    /// Gets a display attribute for a specific achievement (e.g., "name", "desc").
    /// </summary>
    public string GetAchievementDisplayAttribute(string name, string key)
    {
        IntPtr valuePtr = SteamNative.SteamAPI_ISteamUserStats_GetAchievementDisplayAttribute(
            _client.GetUserStatsPointer(), name, key);
        return valuePtr != IntPtr.Zero
            ? System.Runtime.InteropServices.Marshal.PtrToStringAnsi(valuePtr) ?? string.Empty
            : string.Empty;
    }

    /// <summary>
    /// Gets the icon handle for a specific achievement.
    /// Returns 0 if the icon is not ready yet.
    /// </summary>
    public int GetAchievementIcon(string name)
    {
        return SteamNative.SteamAPI_ISteamUserStats_GetAchievementIcon(
            _client.GetUserStatsPointer(), name);
    }

    /// <summary>
    /// Shows a progress indicator for an achievement in the Steam Overlay.
    /// </summary>
    public bool IndicateAchievementProgress(string name, uint currentProgress, uint maxProgress)
    {
        return SteamNative.SteamAPI_ISteamUserStats_IndicateAchievementProgress(
            _client.GetUserStatsPointer(), name, currentProgress, maxProgress);
    }

    /// <summary>
    /// Gets all achievements for the current app.
    /// </summary>
    public List<AchievementInfo> GetAllAchievements()
    {
        var achievements = new List<AchievementInfo>();
        uint count = GetAchievementCount();

        for (uint i = 0; i < count; i++)
        {
            string apiName = GetAchievementName(i);
            if (string.IsNullOrEmpty(apiName))
                continue;

            GetAchievement(apiName, out bool isUnlocked);
            GetAchievementAndUnlockTime(apiName, out _, out uint unlockTime);
            string displayName = GetAchievementDisplayAttribute(apiName, "name");
            string description = GetAchievementDisplayAttribute(apiName, "desc");
            string hidden = GetAchievementDisplayAttribute(apiName, "hidden");
            int iconHandle = GetAchievementIcon(apiName);

            achievements.Add(new AchievementInfo
            {
                ApiName = apiName,
                DisplayName = displayName,
                Description = description,
                IsUnlocked = isUnlocked,
                UnlockTime = unlockTime,
                IconHandle = iconHandle,
                IsHidden = hidden == "1"
            });
        }

        return achievements;
    }
}
