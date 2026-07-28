using SteamManager.Models;

namespace SteamManager.Steam;

public interface ISteamAchievements
{
    bool SetAchievement(string name, int permission);
    bool ClearAchievement(string name, int permission);
    IEnumerable<AchievementInfo> GetAllAchievements();
    bool RequestGlobalAchievementPercentages();
    float GetAchievementAchievedPercent(string name);
}

public interface ISteamStats
{
    bool StoreStats();
    bool ResetAllStats(bool resetAchievements);
    bool GetStat(string name, out int value);
    bool GetStat(string name, out float value);
    bool SetStat(string name, int value);
    bool SetStat(string name, float value);
    bool RequestCurrentStats();
}
