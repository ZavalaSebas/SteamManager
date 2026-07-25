using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SteamManager.Models;

public partial class GameInfo : ObservableObject
{
    public uint AppId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PlaytimeMinutes { get; set; }
    public string? CoverUrl { get; set; }
    public string? HeaderImageUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? ImgIconUrl { get; set; }

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private BitmapImage? _coverImage;
}
