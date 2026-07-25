namespace SteamManager.Steam;

/// <summary>
/// Callback message IDs from Steam API.
/// These are the base IDs for each callback group.
/// </summary>
public static class SteamCallbackIds
{
    public const int UserStatsReceived = 1101;
    public const int UserStatsStored = 1102;
    public const int UserAchievementStored = 1103;
    public const int UserAchievementIconFetched = 1109;
}
