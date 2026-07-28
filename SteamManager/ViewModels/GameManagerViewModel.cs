using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
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
    private readonly GameSchemaService? _gameSchemaService;
    private readonly ILogger<GameManagerViewModel>? _logger;
    private bool _schemaLoadFailed;
    private ObservableCollection<AchievementInfo> _allAchievements = new();
    private System.Threading.CancellationTokenSource? _smartUnlockCts;
    private readonly IMessageBoxService _messageBoxService;
    private readonly ISmartUnlockService _smartUnlockService;

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

    [ObservableProperty]
    private bool _isSmartUnlockRunning;

    [ObservableProperty]
    private bool _isSmartUnlockUiBlocked;

    [ObservableProperty]
    private int _smartUnlockProcessed;

    [ObservableProperty]
    private int _smartUnlockTotal;

    [ObservableProperty]
    private int _smartUnlockAppliedCount;

    [ObservableProperty]
    private int _smartUnlockProtectedCount;

    [ObservableProperty]
    private int _smartUnlockFailedCount;

    [ObservableProperty]
    private int _smartUnlockProgressPercent;

    public bool IsSmartUnlockOverlayVisible => IsSmartUnlockRunning;

    public string SmartUnlockStatusMessage { get; private set; } = string.Empty;

    public bool SmartUnlockWasCancelled { get; private set; }

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

    public GameManagerViewModel(SteamContext steamContext, IImageCacheService? imageCacheService = null, ILogger<GameManagerViewModel>? logger = null, IMessageBoxService? messageBoxService = null, ISmartUnlockService? smartUnlockService = null)
    {
        _steamContext = steamContext;
        _imageCacheService = imageCacheService;
        _logger = logger;
        _messageBoxService = messageBoxService ?? new MessageBoxService();
        _smartUnlockService = smartUnlockService ?? new SmartUnlockService(_steamContext.Achievements, _steamContext.Stats);
        string? steamPath = SteamLoader.GetSteamInstallPath();
        if (!string.IsNullOrEmpty(steamPath))
        {
            _gameSchemaService = new GameSchemaService(steamPath);
        }
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

                if (_gameSchemaService != null && SelectedGame != null)
                {
                    var (schemaAchievements, _) = _gameSchemaService.LoadSchema(SelectedGame.AppId);

                    if (schemaAchievements.Count == 0)
                    {
                        _schemaLoadFailed = true;
                        _logger?.LogWarning("[GameSchema] Failed to load schema for app {AppId} - protection status could not be verified", SelectedGame.AppId);
                    }
                    else
                    {
                        _schemaLoadFailed = false;
                        var schemaDict = schemaAchievements.ToDictionary(s => s.Id, s => s.Permission);
                        int unmatched = 0;
                        foreach (var ach in achievements)
                        {
                            if (schemaDict.TryGetValue(ach.ApiName, out int permission))
                            {
                                ach.Permission = permission;
                                ach.PermissionVerified = true;
                            }
                            else
                            {
                                ach.PermissionVerified = false;
                                unmatched++;
                            }
                        }
                        if (unmatched > 0)
                        {
                            _logger?.LogWarning("[GameSchema] {Unmatched}/{Total} achievements had no matching schema entry", unmatched, achievements.Count());
                        }
                    }
                }

                _allAchievements = new ObservableCollection<AchievementInfo>(achievements);
                TotalCount = achievements.Count();
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
        if (_schemaLoadFailed)
        {
            StatusMessage = "Warning: Could not verify achievement protection status - schema not loaded";
            return;
        }

        if (achievement.IsUnverified)
        {
            StatusMessage = $"Could not verify protection status for '{achievement.DisplayName}' - skipping";
            return;
        }

        if (achievement.IsProtected)
        {
            StatusMessage = $"Achievement '{achievement.DisplayName}' is protected and cannot be modified";
            return;
        }

        try
        {
            bool success;
            if (achievement.IsUnlocked)
            {
                success = _steamContext.Achievements.ClearAchievement(achievement.ApiName, achievement.Permission);
            }
            else
            {
                success = _steamContext.Achievements.SetAchievement(achievement.ApiName, achievement.Permission);
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
        if (_schemaLoadFailed)
        {
            StatusMessage = "Warning: Could not verify achievement protection status - schema not loaded";
            return;
        }

        try
        {
            var targetAchievements = _allAchievements.Where(a => a.IsSelected).Any()
                ? _allAchievements.Where(a => a.IsSelected)
                : _allAchievements;

            int count = 0;
            int protectedSkipped = 0;
            int unverifiedSkipped = 0;
            foreach (var ach in targetAchievements)
            {
                if (!ach.IsUnlocked)
                    continue;

                if (ach.IsProtected || ach.IsUnverified)
                {
                    if (ach.IsUnverified)
                        unverifiedSkipped++;
                    else
                        protectedSkipped++;
                    continue;
                }

                if (_steamContext.Achievements.ClearAchievement(ach.ApiName, ach.Permission))
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
                string msg = $"Locked {count} achievements";
                if (protectedSkipped > 0)
                {
                    msg += $" ({protectedSkipped} protected skipped)";
                }
                if (unverifiedSkipped > 0)
                {
                    msg += $" ({unverifiedSkipped} unverified skipped)";
                }
                StatusMessage = msg;
                ApplyFilter();
                OnPropertyChanged(nameof(SelectedCount));
            }
            else if (protectedSkipped > 0 || unverifiedSkipped > 0)
            {
                string msg = "No achievements locked";
                if (protectedSkipped > 0)
                    msg += $" ({protectedSkipped} protected)";
                if (unverifiedSkipped > 0)
                    msg += $" ({unverifiedSkipped} unverified)";
                StatusMessage = msg;
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
        if (_schemaLoadFailed)
        {
            StatusMessage = "Warning: Could not verify achievement protection status - schema not loaded";
            return;
        }

        try
        {
            var targetAchievements = _allAchievements.Where(a => a.IsSelected).Any()
                ? _allAchievements.Where(a => a.IsSelected)
                : _allAchievements;

            int count = 0;
            int protectedSkipped = 0;
            int unverifiedSkipped = 0;
            foreach (var ach in targetAchievements)
            {
                if (ach.IsUnlocked)
                    continue;

                if (ach.IsProtected || ach.IsUnverified)
                {
                    if (ach.IsUnverified)
                        unverifiedSkipped++;
                    else
                        protectedSkipped++;
                    continue;
                }

                if (_steamContext.Achievements.SetAchievement(ach.ApiName, ach.Permission))
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
                string msg = $"Unlocked {count} achievements";
                if (protectedSkipped > 0)
                {
                    msg += $" ({protectedSkipped} protected skipped)";
                }
                if (unverifiedSkipped > 0)
                {
                    msg += $" ({unverifiedSkipped} unverified skipped)";
                }
                StatusMessage = msg;
                ApplyFilter();
                OnPropertyChanged(nameof(SelectedCount));
            }
            else if (protectedSkipped > 0 || unverifiedSkipped > 0)
            {
                string msg = "No achievements unlocked";
                if (protectedSkipped > 0)
                    msg += $" ({protectedSkipped} protected)";
                if (unverifiedSkipped > 0)
                    msg += $" ({unverifiedSkipped} unverified)";
                StatusMessage = msg;
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
    private async Task ResetAllStats()
    {
        try
        {
            var firstResult = System.Windows.MessageBox.Show(
                "Are you sure you want to reset all statistics?\nThis cannot be undone.",
                "Reset Stats",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (firstResult != System.Windows.MessageBoxResult.Yes)
                return;

            var achievementsResult = System.Windows.MessageBox.Show(
                "Do you also want to reset achievements?\nThis cannot be undone.",
                "Reset Achievements",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            bool resetAchievements = achievementsResult == System.Windows.MessageBoxResult.Yes;

            var finalResult = System.Windows.MessageBox.Show(
                "Are you absolutely sure?\nAll progress will be permanently lost.",
                "Final Confirmation",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Error);

            if (finalResult != System.Windows.MessageBoxResult.Yes)
                return;

            if (_steamContext.Stats.ResetAllStats(resetAchievements))
            {
                StatName = string.Empty;
                StatValue = string.Empty;
                StatusMessage = resetAchievements
                    ? "All stats and achievements reset. Reopen the stats editor or reselect the stat to see updated values."
                    : "All stats reset. Reopen the stats editor or reselect the stat to see updated values.";

                await LoadAchievementsAsync();
            }
            else
            {
                StatusMessage = "Failed to reset stats - Steam may have blocked the operation";
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

    public bool TryNavigateDuringSmartUnlock(out bool userCancelledSmartUnlock)
    {
        userCancelledSmartUnlock = false;
        if (!IsSmartUnlockRunning)
            return true;

        int processed = SmartUnlockAppliedCount + SmartUnlockProtectedCount + SmartUnlockFailedCount;
        var result = _messageBoxService.Show(
            $"Switching games will cancel the current operation.\n{processed} achievements have already been processed.\n\n[Stay] Keep Smart Unlock running\n[Switch and Cancel] Cancel and switch games",
            "Smart Unlock in Progress",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
            return false;

        _smartUnlockCts?.Cancel();
        userCancelledSmartUnlock = true;
        return true;
    }

    public void CancelSmartUnlock()
    {
        _smartUnlockCts?.Cancel();
    }

    [RelayCommand(CanExecute = nameof(CanExecuteSmartUnlock))]
    private async Task SmartUnlockAsync()
    {
        await ExecuteSmartUnlockAsync(500, 1500);
    }

    public async Task ExecuteSmartUnlockAsync(int minDelayMs, int maxDelayMs, bool showOverlay = true)
    {
        if (SelectedGame == null)
            return;

        var targetAchievements = _allAchievements.Where(a => a.IsSelected).Any()
            ? _allAchievements.Where(a => a.IsSelected).ToList()
            : _allAchievements.ToList();

        if (targetAchievements.Count == 0)
        {
            StatusMessage = "No achievements selected";
            return;
        }

        IsSmartUnlockRunning = true;
        IsSmartUnlockUiBlocked = true;
        _smartUnlockCts = new System.Threading.CancellationTokenSource();
        SmartUnlockAppliedCount = 0;
        SmartUnlockProtectedCount = 0;
        SmartUnlockFailedCount = 0;
        SmartUnlockProgressPercent = 0;
        SmartUnlockWasCancelled = false;
        SmartUnlockTotal = targetAchievements.Count;
        SmartUnlockStatusMessage = "Smart Unlock: 0/" + targetAchievements.Count + " achievements processed (0 protected, 0 failed)";

        var achList = targetAchievements
            .Select(a => (a.ApiName, a.Permission))
            .ToList();

        try
        {
            var progress = new Progress<SmartUnlockProgress>(p =>
            {
                SmartUnlockStatusMessage = $"Smart Unlock: {p.Processed}/{p.Total} achievements processed ({p.Protected} protected, {p.Failed} failed)";
                SmartUnlockProgressPercent = p.Total > 0 ? (int)((double)p.Processed / p.Total * 100) : 0;
                SmartUnlockProcessed = p.Processed;
                SmartUnlockAppliedCount = p.Applied;
                SmartUnlockProtectedCount = p.Protected;
                SmartUnlockFailedCount = p.Failed;
            });

            var result = await _smartUnlockService.UnlockAchievementsAsync(
                achList,
                TimeSpan.FromMilliseconds(minDelayMs),
                TimeSpan.FromMilliseconds(maxDelayMs),
                progress,
                _smartUnlockCts.Token);

            SmartUnlockProgressPercent = 100;
            SmartUnlockProcessed = result.Applied + result.Protected + result.Failed;
            SmartUnlockAppliedCount = result.Applied;
            SmartUnlockProtectedCount = result.Protected;
            SmartUnlockFailedCount = result.Failed;

            bool hasProblems = result.Protected > 0 || result.Failed > 0;
            if (hasProblems)
            {
                SmartUnlockStatusMessage = $"Smart Unlock complete: {result.Applied} applied, {result.Protected} protected, {result.Failed} failed";
            }
            else
            {
                SmartUnlockStatusMessage = result.Applied > 0
                    ? $"Smart Unlock complete: {result.Applied} achievements unlocked"
                    : "Smart Unlock: no achievements to unlock";
            }

            foreach (var ach in _allAchievements)
            {
                if (achList.Any(x => x.Item1 == ach.ApiName))
                {
                    if (ach.Permission == 0)
                    {
                        ach.IsUnlocked = true;
                    }
                }
            }
            UnlockedCount = _allAchievements.Count(a => a.IsUnlocked);
            ApplyFilter();

            return;
        }
        catch (OperationCanceledException)
        {
            SmartUnlockWasCancelled = true;
            SmartUnlockStatusMessage = $"Smart Unlock cancelled: {SmartUnlockAppliedCount} applied, {SmartUnlockProtectedCount} protected, {SmartUnlockFailedCount} failed";
        }
        catch (Exception ex)
        {
            SmartUnlockStatusMessage = $"Smart Unlock error: {ex.Message}";
        }
        finally
        {
            IsSmartUnlockRunning = false;
            IsSmartUnlockUiBlocked = false;
            _smartUnlockCts?.Dispose();
            _smartUnlockCts = null;
        }
    }

    private bool CanExecuteSmartUnlock()
    {
        if (IsSmartUnlockRunning)
            return false;
        if (_schemaLoadFailed)
            return false;
        if (_allAchievements.Count == 0)
            return false;
        return true;
    }
}
