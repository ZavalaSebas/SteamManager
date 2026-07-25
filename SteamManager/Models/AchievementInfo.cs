namespace SteamManager.Models;

public class AchievementInfo
{
    public string ApiName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
    public uint UnlockTime { get; set; }
    public int IconHandle { get; set; }
    public bool IsHidden { get; set; }
}
