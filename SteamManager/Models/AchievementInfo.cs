using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace SteamManager.Models;

public class AchievementInfo : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string ApiName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    private bool _isUnlocked;
    public bool IsUnlocked
    {
        get => _isUnlocked;
        set
        {
            if (_isUnlocked != value)
            {
                _isUnlocked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUnlocked)));
            }
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    public uint UnlockTime { get; set; }
    public int IconHandle { get; set; }
    public bool IsHidden { get; set; }
    public int Permission { get; set; }
    public bool PermissionVerified { get; set; }
    public BitmapSource? Icon { get; set; }
    public string? IconUrl { get; set; }
    public string? IconLockedUrl { get; set; }

    private float _globalPercentage = -1f;
    public float GlobalPercentage
    {
        get => _globalPercentage;
        set
        {
            if (Math.Abs(_globalPercentage - value) > 0.001f)
            {
                _globalPercentage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GlobalPercentage)));
            }
        }
    }

    public bool IsProtected => PermissionVerified && (Permission & 3) != 0;
    public bool IsUnverified => !PermissionVerified;

    public void NotifyIconChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
}
