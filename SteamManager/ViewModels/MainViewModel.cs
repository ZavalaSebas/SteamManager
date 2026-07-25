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
    private GamePickerViewModel? _gamePicker;

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
            _gamePicker = new GamePickerViewModel(_steamContext, _gameLibraryService, _imageCacheService);
            _gamePicker.GameSelected += OnGameSelected;
            CurrentViewModel = _gamePicker;
            await _gamePicker.LoadGamesCommand.ExecuteAsync(null);
            StatusMessage = _gamePicker.StatusMessage;
        }
        else
        {
            StatusMessage = "Steam not connected. Some features may be unavailable.";
        }
    }

    private void OnGameSelected(GameInfo game)
    {
        var managerVm = new GameManagerViewModel(_steamContext, game, _imageCacheService);
        CurrentViewModel = managerVm;
        StatusMessage = $"Viewing: {game.Name}";
    }

    [RelayCommand]
    private void BackToGames()
    {
        if (_gamePicker != null)
        {
            CurrentViewModel = _gamePicker;
            StatusMessage = _gamePicker.StatusMessage;
        }
    }
}
