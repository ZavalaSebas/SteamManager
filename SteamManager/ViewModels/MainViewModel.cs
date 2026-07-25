using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamManager.Models;
using SteamManager.Services;
using SteamManager.Steam;

namespace SteamManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SteamContext _steamContext;
    private readonly IGameLibraryService _gameLibraryService;
    private readonly IImageCacheService _imageCacheService;
    private readonly IConfigService _configService;
    private GamePickerViewModel? _gamePicker;
    private GameManagerViewModel? _gameManager;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private string _statusMessage = "Initializing...";

    [ObservableProperty]
    private bool _isLoading;

    public MainViewModel(
        SteamContext steamContext,
        IGameLibraryService gameLibraryService,
        IImageCacheService imageCacheService,
        IConfigService configService)
    {
        _steamContext = steamContext;
        _gameLibraryService = gameLibraryService;
        _imageCacheService = imageCacheService;
        _configService = configService;
    }

    [RelayCommand]
    private async Task LoadGamesAsync()
    {
        _gamePicker = new GamePickerViewModel(_steamContext, _gameLibraryService, _imageCacheService, _configService)
        {
            OnGameSelected = NavigateToGame
        };
        CurrentViewModel = _gamePicker;
        await _gamePicker.LoadGamesCommand.ExecuteAsync(null);
        StatusMessage = _gamePicker.StatusMessage;
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

    private void NavigateToGame(GameInfo game)
    {
        _gameManager = new GameManagerViewModel(_steamContext, _imageCacheService);
        _gameManager.SelectGameCommand.Execute(game);
        CurrentViewModel = _gameManager;
        StatusMessage = $"Loading {game.Name}...";
        _gameManager.LoadAchievementsCommand.Execute(null);
    }
}
