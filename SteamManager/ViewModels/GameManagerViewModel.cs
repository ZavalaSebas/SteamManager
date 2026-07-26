using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamManager.Models;
using SteamManager.Services;
using SteamManager.Steam;

namespace SteamManager.ViewModels;

public enum AchievementFilterType
{
    All,
    Unlocked,
    Locked,
    Hidden
}

public partial class GameManagerViewModel : ObservableObject
{
    private readonly SteamContext _steamContext;
    private readonly IImageCacheService? _imageCacheService;
    private ObservableCollection<AchievementInfo> _allAchievements = new();

    [ObservableProperty]
    private GameInfo? _selectedGame;

    [ObservableProperty]
    private ObservableCollection<AchievementInfo> _achievements = new();

    [ObservableProperty]
    private AchievementFilterType _achievementFilter = AchievementFilterType.All;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isStatsEditorExpanded;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _unlockedCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _statName = string.Empty;

    [ObservableProperty]
    private string _statValue = string.Empty;

    [ObservableProperty]
    private string[] _availableStats = Array.Empty<string>();

    [ObservableProperty]
    private string? _selectedStat;

    public int SelectedCount => _allAchievements.Count(a => a.IsSelected);

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var ach in _allAchievements)
        {
            ach.IsSelected = true;
        }
        OnPropertyChanged(nameof(SelectedCount));
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var ach in _allAchievements)
        {
            ach.IsSelected = false;
        }
        OnPropertyChanged(nameof(SelectedCount));
    }

    public IEnumerable<AchievementInfo> FilteredAchievements => GetFilteredAchievements();

    private IEnumerable<AchievementInfo> GetFilteredAchievements()
    {
        return AchievementFilter switch
        {
            AchievementFilterType.Unlocked => _allAchievements.Where(a => a.IsUnlocked),
            AchievementFilterType.Locked => _allAchievements.Where(a => !a.IsUnlocked),
            AchievementFilterType.Hidden => _allAchievements.Where(a => a.IsHidden),
            _ => _allAchievements
        };
    }

    partial void OnAchievementFilterChanged(AchievementFilterType value)
    {
        Achievements = new ObservableCollection<AchievementInfo>(GetFilteredAchievements());
    }

    private void ApplyFilter()
    {
        Achievements = new ObservableCollection<AchievementInfo>(GetFilteredAchievements());
    }

    public GameManagerViewModel(SteamContext steamContext, IImageCacheService? imageCacheService = null)
    {
        _steamContext = steamContext;
        _imageCacheService = imageCacheService;
    }

    [RelayCommand]
    private async Task LoadAchievementsAsync()
    {
        if (SelectedGame == null) return;

        IsLoading = true;
        StatusMessage = $"Loading {SelectedGame.Name}...";

        try
        {
            var achievements = await Task.Run(() =>
            {
                if (!_steamContext.IsInitialized)
                {
                    _steamContext.ChangeAppId(SelectedGame.AppId);
                }

                if (!_steamContext.IsInitialized)
                {
                    return null;
                }

                return _steamContext.Achievements.GetAllAchievements();
            });

            if (achievements == null)
            {
                StatusMessage = "Failed to load achievements";
                _allAchievements = new ObservableCollection<AchievementInfo>();
                Achievements = new ObservableCollection<AchievementInfo>();
                TotalCount = 0;
                UnlockedCount = 0;
            }
            else
            {
                foreach (var ach in achievements)
                {
                    if (ach.IconHandle != 0)
                    {
                        ach.Icon = _steamContext.Icons.GetBitmapSource(ach.IconHandle);
                    }
                }

                _allAchievements = new ObservableCollection<AchievementInfo>(achievements);
                TotalCount = achievements.Count;
                UnlockedCount = achievements.Count(a => a.IsUnlocked);
                StatusMessage = $"{UnlockedCount}/{TotalCount} achievements unlocked";

                ApplyFilter();
                _ = LoadIconsFromCdnAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadIconsFromCdnAsync()
    {
        if (_imageCacheService == null || Achievements == null || SelectedGame == null)
            return;

        foreach (var ach in Achievements)
        {
            if (ach.Icon != null && ach.Icon.CanFreeze)
                continue;

            if (string.IsNullOrEmpty(ach.IconUrl))
                continue;

            try
            {
                string iconName = ach.IsUnlocked ? ach.IconUrl! : (ach.IconLockedUrl ?? ach.IconUrl!);
                string iconCdnUrl = $"https://cdn.steamstatic.com/steamcommunity/public/images/apps/{SelectedGame.AppId}/{iconName}";
                var icon = await _imageCacheService.GetOrDownloadAsync(iconCdnUrl);
                if (icon != null)
                {
                    ach.Icon = icon;
                    ach.NotifyIconChanged();
                }
            }
            catch { }
        }
    }

    private async Task RefreshAchievementIconAsync(AchievementInfo achievement)
    {
        if (_imageCacheService == null || SelectedGame == null)
            return;

        if (string.IsNullOrEmpty(achievement.IconUrl))
            return;

        try
        {
            string iconName = achievement.IsUnlocked
                ? achievement.IconUrl!
                : (achievement.IconLockedUrl ?? achievement.IconUrl!);
            string iconCdnUrl = $"https://cdn.steamstatic.com/steamcommunity/public/images/apps/{SelectedGame.AppId}/{iconName}";
            var icon = await _imageCacheService.GetOrDownloadAsync(iconCdnUrl);
            if (icon != null)
            {
                achievement.Icon = icon;
                achievement.NotifyIconChanged();
            }
        }
        catch { }
    }

    [RelayCommand]
    public void SelectGame(GameInfo? game)
    {
        if (game == null) return;
        SelectedGame = game;
        AvailableStats = GameStats.GetStatsForGame(game.AppId);
    }

    [RelayCommand]
    private void ToggleAchievement(AchievementInfo achievement)
    {
        try
        {
            bool success;
            if (achievement.IsUnlocked)
            {
                success = _steamContext.Achievements.ClearAchievement(achievement.ApiName);
            }
            else
            {
                success = _steamContext.Achievements.SetAchievement(achievement.ApiName);
            }

            if (success)
            {
                achievement.IsUnlocked = !achievement.IsUnlocked;
                UnlockedCount = _allAchievements.Count(a => a.IsUnlocked);
                StatusMessage = $"{UnlockedCount}/{TotalCount} achievements unlocked";
                _steamContext.Stats.StoreStats();

                _ = RefreshAchievementIconAsync(achievement);

                if (AchievementFilter != AchievementFilterType.All)
                {
                    ApplyFilter();
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error toggling achievement: {ex.Message}";
        }
    }

    [RelayCommand]
    private void StoreStats()
    {
        try
        {
            _steamContext.Stats.StoreStats();
            StatusMessage = "Stats stored successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error storing stats: {ex.Message}";
        }
    }

    [RelayCommand]
    private void LockAll()
    {
        try
        {
            var targetAchievements = _allAchievements.Where(a => a.IsSelected).Any()
                ? _allAchievements.Where(a => a.IsSelected)
                : _allAchievements;

            int count = 0;
            foreach (var ach in targetAchievements)
            {
                if (!ach.IsUnlocked)
                    continue;

                if (_steamContext.Achievements.ClearAchievement(ach.ApiName))
                {
                    ach.IsUnlocked = false;
                    ach.IsSelected = false;
                    count++;
                    _ = RefreshAchievementIconAsync(ach);
                }
            }

            if (count > 0)
            {
                _steamContext.Stats.StoreStats();
                UnlockedCount = _allAchievements.Count(a => a.IsUnlocked);
                StatusMessage = $"Locked {count} achievements";
                ApplyFilter();
                OnPropertyChanged(nameof(SelectedCount));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error locking achievements: {ex.Message}";
        }
    }

    [RelayCommand]
    private void UnlockAll()
    {
        try
        {
            var targetAchievements = _allAchievements.Where(a => a.IsSelected).Any()
                ? _allAchievements.Where(a => a.IsSelected)
                : _allAchievements;

            int count = 0;
            foreach (var ach in targetAchievements)
            {
                if (ach.IsUnlocked)
                    continue;

                if (_steamContext.Achievements.SetAchievement(ach.ApiName))
                {
                    ach.IsUnlocked = true;
                    ach.IsSelected = false;
                    count++;
                    _ = RefreshAchievementIconAsync(ach);
                }
            }

            if (count > 0)
            {
                _steamContext.Stats.StoreStats();
                UnlockedCount = _allAchievements.Count(a => a.IsUnlocked);
                StatusMessage = $"Unlocked {count} achievements";
                ApplyFilter();
                OnPropertyChanged(nameof(SelectedCount));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error unlocking achievements: {ex.Message}";
        }
    }

    [RelayCommand]
    private void GetStat()
    {
        if (string.IsNullOrWhiteSpace(StatName))
        {
            StatusMessage = "Enter a stat name first";
            return;
        }

        try
        {
            if (_steamContext.Stats.GetStat(StatName, out int intValue))
            {
                StatValue = intValue.ToString();
                StatusMessage = $"Got stat: {StatName} = {intValue}";
            }
            else if (_steamContext.Stats.GetStat(StatName, out float floatValue))
            {
                StatValue = floatValue.ToString("F2");
                StatusMessage = $"Got stat: {StatName} = {floatValue:F2}";
            }
            else
            {
                StatValue = string.Empty;
                StatusMessage = $"Stat '{StatName}' not found or not available";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error getting stat: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SetStat()
    {
        if (string.IsNullOrWhiteSpace(StatName))
        {
            StatusMessage = "Enter a stat name first";
            return;
        }

        try
        {
            if (int.TryParse(StatValue, out int intValue))
            {
                if (_steamContext.Stats.SetStat(StatName, intValue))
                {
                    StatusMessage = $"Set {StatName} = {intValue}";
                }
                else
                {
                    StatusMessage = $"Failed to set stat '{StatName}'";
                }
            }
            else if (float.TryParse(StatValue, out float floatValue))
            {
                if (_steamContext.Stats.SetStat(StatName, floatValue))
                {
                    StatusMessage = $"Set {StatName} = {floatValue:F2}";
                }
                else
                {
                    StatusMessage = $"Failed to set stat '{StatName}'";
                }
            }
            else
            {
                StatusMessage = "Invalid value. Enter a number.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error setting stat: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ResetAllStats()
    {
        try
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to reset ALL stats?\nThis cannot be undone.",
                "Reset Stats",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                if (_steamContext.Stats.ResetAllStats(false))
                {
                    StatName = string.Empty;
                    StatValue = string.Empty;
                    StatusMessage = "All stats reset";
                }
                else
                {
                    StatusMessage = "Failed to reset stats";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error resetting stats: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SetFilterAll() => AchievementFilter = AchievementFilterType.All;

    [RelayCommand]
    private void SetFilterUnlocked() => AchievementFilter = AchievementFilterType.Unlocked;

    [RelayCommand]
    private void SetFilterLocked() => AchievementFilter = AchievementFilterType.Locked;

    [RelayCommand]
    private void SetFilterHidden() => AchievementFilter = AchievementFilterType.Hidden;

    [RelayCommand]
    private void Back()
    {
        System.Windows.Application.Current.Shutdown();
    }
}
