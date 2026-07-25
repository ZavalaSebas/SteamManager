using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamManager.Models;
using SteamManager.Services;
using SteamManager.Steam;

namespace SteamManager.ViewModels;

public partial class GamePickerViewModel : ObservableObject
{
    private readonly SteamContext _steamContext;
    private readonly IGameLibraryService _gameLibraryService;
    private readonly IImageCacheService _imageCacheService;
    private readonly IConfigService _configService;
    private List<GameInfo> _allGames = new();

    [ObservableProperty]
    private ObservableCollection<GameInfo> _games = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public Action<GameInfo>? OnGameSelected { get; set; }

    public GamePickerViewModel(
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
        IsLoading = true;
        StatusMessage = "Loading games...";

        try
        {
            _allGames = await _gameLibraryService.GetOwnedGamesAsync();

            var favoriteIds = _configService.FavoriteGameIds;
            foreach (var game in _allGames)
            {
                game.IsFavorite = favoriteIds.Contains(game.AppId);
            }

            var recentIds = _configService.RecentlyOpenedGameIds;
            var sortedGames = _allGames
                .OrderByDescending(g => g.IsFavorite)
                .ThenBy(g => GetRecentIndex(g.AppId, recentIds))
                .ThenBy(g => g.Name)
                .ToList();

            Games = new ObservableCollection<GameInfo>(sortedGames);
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
        var recentIds = _configService.RecentlyOpenedGameIds;

        IEnumerable<GameInfo> sorted;
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            sorted = _allGames
                .OrderByDescending(g => g.IsFavorite)
                .ThenBy(g => GetRecentIndex(g.AppId, recentIds))
                .ThenBy(g => g.Name);
        }
        else
        {
            sorted = _allGames
                .Where(g => g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(g => g.IsFavorite)
                .ThenBy(g => GetRecentIndex(g.AppId, recentIds))
                .ThenBy(g => g.Name);
        }
        Games = new ObservableCollection<GameInfo>(sorted.ToList());
    }

    private static int GetRecentIndex(uint appId, List<uint> recentIds)
    {
        int index = recentIds.IndexOf(appId);
        return index < 0 ? int.MaxValue : index;
    }

    partial void OnSearchTextChanged(string value)
    {
        SearchGames();
    }

    [RelayCommand]
    private void ToggleFavorite(GameInfo? game)
    {
        if (game == null) return;

        game.IsFavorite = !game.IsFavorite;

        if (game.IsFavorite)
        {
            _configService.AddFavorite(game.AppId);
        }
        else
        {
            _configService.RemoveFavorite(game.AppId);
        }
        _configService.Save();

        SearchGames();
    }

    [RelayCommand]
    private async Task SelectGameAsync(GameInfo? game)
    {
        if (game == null) return;

        _configService.MarkRecentlyOpened(game.AppId);
        _configService.Save();

        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? System.IO.Path.Combine(System.AppContext.BaseDirectory, "SteamManager.exe");

        var helperProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"--game {game.AppId}",
            UseShellExecute = true
        });

        if (helperProcess != null)
        {
            await helperProcess.WaitForExitAsync();
        }

        RefreshGameStates();
        SearchGames();
    }

    private void RefreshGameStates()
    {
        var favoriteIds = _configService.FavoriteGameIds;
        foreach (var game in _allGames)
        {
            game.IsFavorite = favoriteIds.Contains(game.AppId);
        }
    }
}
