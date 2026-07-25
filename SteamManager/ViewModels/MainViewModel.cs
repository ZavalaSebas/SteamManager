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

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private string _statusMessage = "Initializing...";

    [ObservableProperty]
    private bool _isLoading;

    public MainViewModel(SteamContext steamContext, IGameLibraryService gameLibraryService)
    {
        _steamContext = steamContext;
        _gameLibraryService = gameLibraryService;
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
            var gamePicker = new GamePickerViewModel(_steamContext, _gameLibraryService);
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
