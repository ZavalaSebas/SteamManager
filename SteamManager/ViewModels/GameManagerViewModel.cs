using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamManager.Models;
using SteamManager.Services;
using SteamManager.Steam;

namespace SteamManager.ViewModels;

/// <summary>
/// ViewModel for managing achievements and stats of a single game.
/// </summary>
public partial class GameManagerViewModel : ObservableObject
{
    private readonly SteamContext _steamContext;
    private readonly IImageCacheService? _imageCacheService;

    [ObservableProperty]
    private GameInfo? _selectedGame;

    [ObservableProperty]
    private ObservableCollection<AchievementInfo> _achievements = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _unlockedCount;

    [ObservableProperty]
    private int _totalCount;

    public event Action? RequestBack;

    public GameManagerViewModel(SteamContext steamContext, GameInfo game, IImageCacheService? imageCacheService = null)
    {
        _steamContext = steamContext;
        _imageCacheService = imageCacheService;
        SelectedGame = game;
    }

    [RelayCommand]
    private async Task LoadAchievementsAsync()
    {
        if (SelectedGame == null) return;

        IsLoading = true;
        StatusMessage = "Loading achievements...";

        try
        {
            await Task.Run(() =>
            {
                var achievements = _steamContext.Achievements.GetAllAchievements();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Achievements = new ObservableCollection<AchievementInfo>(achievements);
                    TotalCount = achievements.Count;
                    UnlockedCount = achievements.Count(a => a.IsUnlocked);
                });
            });

            StatusMessage = $"{UnlockedCount}/{TotalCount} achievements unlocked";
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
                UnlockedCount = Achievements.Count(a => a.IsUnlocked);
                StatusMessage = $"{UnlockedCount}/{TotalCount} achievements unlocked";
                _steamContext.Stats.StoreStats();
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
    private void Back()
    {
        RequestBack?.Invoke();
    }
}
