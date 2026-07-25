namespace SteamManager.Services;

public static class GameStats
{
    public static readonly Dictionary<uint, string[]> PopularGameStats = new()
    {
        { 440, new[] { "miles", "knife_kills", "achievements" } }, // TF2
        { 570, new[] { "miles", "kills", "deaths", "achievements" } }, // Dota 2
        { 730, new[] { "miles", "kills", "deaths", "achievements", "MVPs", "Dominations", "Revenges" } }, // CS2
        { 252490, new[] { "miles", "zombie_kills", "bullets_fired", "bullets_hit", "achievements" } }, // Rust
        { 105600, new[] { "miles", "zombie_kills", "headshots", "deaths", "achievements" } }, // Terraria
        { 374320, new[] { "miles", "zombie_kills", "player_kills", "deaths", "achievements" } }, // Darkwood
        { 238960, new[] { "miles", "zombie_kills", "headshots", "deaths", "achievements" } }, // State of Decay
        { 413150, new[] { "miles", "kills", "deaths", "headshots", "achievements" } }, // Stardew Valley
        { 271590, new[] { "miles", "zombie_kills", "deaths", "headshots", "achievements" } }, // 7 Days to Die
    };

    public static string[] GetStatsForGame(uint appId)
    {
        return PopularGameStats.TryGetValue(appId, out var stats) ? stats : Array.Empty<string>();
    }

    public static bool HasStats(uint appId)
    {
        return PopularGameStats.ContainsKey(appId);
    }
}
