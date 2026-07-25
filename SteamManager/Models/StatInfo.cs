namespace SteamManager.Models;

public enum StatType
{
    Integer,
    Float
}

public class StatInfo
{
    public string Name { get; set; } = string.Empty;
    public StatType Type { get; set; }
    public int IntValue { get; set; }
    public float FloatValue { get; set; }
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
}
