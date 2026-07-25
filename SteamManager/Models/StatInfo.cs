using System.ComponentModel;

namespace SteamManager.Models;

public enum StatType
{
    Integer,
    Float
}

public class StatInfo : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; set; } = string.Empty;
    public StatType Type { get; set; }

    private int _intValue;
    public int IntValue
    {
        get => _intValue;
        set { _intValue = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IntValue))); }
    }

    private float _floatValue;
    public float FloatValue
    {
        get => _floatValue;
        set { _floatValue = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FloatValue))); }
    }

    public int MinValue { get; set; }
    public int MaxValue { get; set; }
}
