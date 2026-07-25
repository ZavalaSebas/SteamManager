using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamManager.Models;
using SteamManager.Services;
using SteamManager.Steam;

namespace SteamManager.ViewModels;

/// <summary>
/// Main ViewModel that acts as the shell for navigation.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly SteamContext _steamContext;
    private readonly IGameLibraryService _gameLibraryService;
    private readonly IImageCacheService _imageCacheService;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private string _statusMessage = "Initializing...";

    [ObservableProperty]
    private bool _isLoading;

    public MainViewModel(
        SteamContext steamContext,
        IGameLibraryService gameLibraryService,
        IImageCacheService imageCacheService)
    {
        _steamContext = steamContext;
        _gameLibraryService = gameLibraryService;
        _imageCacheService = imageCacheService;
    }

    /// <summary>
    /// Called after Steam connection is established.
    /// Loads the game picker view.
    /// </summary>
    [RelayCommand]
    private async Task LoadGamesAsync()
    {
        if (_steamContext.IsInitialized)
        {
            var gamePicker = new GamePickerViewModel(_steamContext, _gameLibraryService, _imageCacheService);
            CurrentViewModel = gamePicker;
            await gamePicker.LoadGamesCommand.ExecuteAsync(null);
            StatusMessage = gamePicker.StatusMessage;
        }
        else
        {
            StatusMessage = "Steam not connected. Some features may be unavailable.";
        }
    }
}
