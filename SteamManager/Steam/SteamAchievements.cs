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
        return _client.UserStats.GetNumAchievements();
    }

    /// <summary>
    /// Gets the API name of the achievement at the specified index.
    /// </summary>
    public string GetAchievementName(uint index)
    {
        return _client.UserStats.GetAchievementName(index);
    }

    /// <summary>
    /// Gets whether a specific achievement is unlocked.
    /// </summary>
    public bool GetAchievement(string name, out bool isUnlocked)
    {
        return _client.UserStats.GetAchievement(name, out isUnlocked);
    }

    /// <summary>
    /// Sets (unlocks) a specific achievement.
    /// </summary>
    public bool SetAchievement(string name)
    {
        return _client.UserStats.SetAchievement(name);
    }

    /// <summary>
    /// Clears (locks) a specific achievement.
    /// </summary>
    public bool ClearAchievement(string name)
    {
        return _client.UserStats.ClearAchievement(name);
    }

    /// <summary>
    /// Gets the unlock state and time for a specific achievement.
    /// </summary>
    public bool GetAchievementAndUnlockTime(string name, out bool isUnlocked, out uint unlockTime)
    {
        return _client.UserStats.GetAchievementAndUnlockTime(name, out isUnlocked, out unlockTime);
    }

    /// <summary>
    /// Gets a display attribute for a specific achievement (e.g., "name", "desc").
    /// </summary>
    public string GetAchievementDisplayAttribute(string name, string key)
    {
        return _client.UserStats.GetAchievementDisplayAttribute(name, key);
    }

    /// <summary>
    /// Gets the icon handle for a specific achievement.
    /// Returns 0 if the icon is not ready yet.
    /// </summary>
    public int GetAchievementIcon(string name)
    {
        return _client.UserStats.GetAchievementIcon(name);
    }

    /// <summary>
    /// Shows a progress indicator for an achievement in the Steam Overlay.
    /// </summary>
    public bool IndicateAchievementProgress(string name, uint currentProgress, uint maxProgress)
    {
        return _client.UserStats.IndicateAchievementProgress(name, currentProgress, maxProgress);
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
            string iconUrl = GetAchievementDisplayAttribute(apiName, "icon");
            string iconLockedUrl = GetAchievementDisplayAttribute(apiName, "icon_gray");
            int iconHandle = GetAchievementIcon(apiName);

            achievements.Add(new AchievementInfo
            {
                ApiName = apiName,
                DisplayName = displayName,
                Description = description,
                IsUnlocked = isUnlocked,
                UnlockTime = unlockTime,
                IconHandle = iconHandle,
                IsHidden = hidden == "1",
                IconUrl = iconUrl,
                IconLockedUrl = iconLockedUrl
            });
        }

        return achievements;
    }
}
