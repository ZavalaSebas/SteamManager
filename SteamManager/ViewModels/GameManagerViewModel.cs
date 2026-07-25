using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamManager.Models;
using SteamManager.Services;
using SteamManager.Steam;

namespace SteamManager.ViewModels;

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

    public GameManagerViewModel(SteamContext steamContext, IImageCacheService? imageCacheService = null)
    {
        _steamContext = steamContext;
        _imageCacheService = imageCacheService;
    }

    [RelayCommand]
    private async Task LoadAchievementsAsync()
    {
        if (SelectedGame == null) return;

        string logFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "steammanager_gamemanager.txt");
        void Log(string msg) => System.IO.File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");

        IsLoading = true;
        StatusMessage = $"Loading {SelectedGame.Name}...";

        try
        {
            await Task.Run(() =>
            {
                Log($"Changing AppId to {SelectedGame.AppId}...");
                bool initResult = _steamContext.ChangeAppId(SelectedGame.AppId);
                Log($"ChangeAppId result: {initResult}, IsInitialized: {_steamContext.IsInitialized}");

                if (!_steamContext.IsInitialized)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = "Failed to initialize Steam for this game";
                        Achievements = new ObservableCollection<AchievementInfo>();
                        TotalCount = 0;
                        UnlockedCount = 0;
                    });
                    return;
                }

                var achievements = _steamContext.Achievements.GetAllAchievements();
                Log($"Got {achievements.Count} achievements");

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
            Log($"Exception: {ex}");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void SelectGame(GameInfo? game)
    {
        if (game == null) return;
        SelectedGame = game;
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
