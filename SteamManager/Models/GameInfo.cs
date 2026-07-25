namespace SteamManager.Models;

public class GameInfo
{
    public uint AppId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PlaytimeMinutes { get; set; }
    public string? CoverUrl { get; set; }
    public bool IsFavorite { get; set; }
}
