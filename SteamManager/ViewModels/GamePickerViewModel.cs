using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamManager.Models;
using SteamManager.Services;
using SteamManager.Steam;

namespace SteamManager.ViewModels;

/// <summary>
/// ViewModel for the game picker view.
/// Displays a virtualized grid of the user's Steam games.
/// </summary>
public partial class GamePickerViewModel : ObservableObject
{
    private readonly SteamContext _steamContext;
    private readonly IGameLibraryService _gameLibraryService;
    private readonly IImageCacheService _imageCacheService;
    private List<GameInfo> _allGames = new();

    [ObservableProperty]
    private ObservableCollection<GameInfo> _games = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public GamePickerViewModel(
        SteamContext steamContext,
        IGameLibraryService gameLibraryService,
        IImageCacheService imageCacheService)
    {
        _steamContext = steamContext;
        _gameLibraryService = gameLibraryService;
        _imageCacheService = imageCacheService;
    }

    [RelayCommand]
    private async Task LoadGamesAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading games...";

        try
        {
            _allGames = await _gameLibraryService.GetOwnedGamesAsync();
            Games = new ObservableCollection<GameInfo>(_allGames);
            StatusMessage = $"{Games.Count} games loaded";

            _ = LoadCoversAsync();
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

    private async Task LoadCoversAsync()
    {
        foreach (var game in _allGames)
        {
            if (game.CoverImage != null || string.IsNullOrEmpty(game.CoverUrl))
                continue;

            var image = await _imageCacheService.GetOrDownloadAsync(game.CoverUrl!);
            if (image != null)
            {
                game.CoverImage = image;
            }
        }
    }

    [RelayCommand]
    private void SearchGames()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Games = new ObservableCollection<GameInfo>(_allGames);
        }
        else
        {
            var filtered = _allGames
                .Where(g => g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Games = new ObservableCollection<GameInfo>(filtered);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        SearchGames();
    }
}
