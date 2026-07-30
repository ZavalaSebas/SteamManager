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

    [ObservableProperty]
    private bool _showGames = true;

    [ObservableProperty]
    private bool _showDemos = false;

    [ObservableProperty]
    private bool _showMods = false;

    [ObservableProperty]
    private bool _showJunk = false;

    [ObservableProperty]
    private string _addGameAppId = string.Empty;

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
        FileLogger.Initialize();

        try
        {
            _allGames = await _gameLibraryService.GetOwnedGamesAsync();

            var favoriteIds = new HashSet<uint>(_configService.FavoriteGameIds);
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

    [RelayCommand]
    private async Task RefreshGames()
    {
        await LoadGamesAsync();
    }

    [RelayCommand]
    private void AddGame()
    {
        if (string.IsNullOrWhiteSpace(AddGameAppId))
        {
            StatusMessage = "Enter an App ID first";
            return;
        }

        if (!uint.TryParse(AddGameAppId.Trim(), out uint appId))
        {
            StatusMessage = "Invalid App ID format";
            return;
        }

        if (!_steamContext.IsInitialized)
        {
            StatusMessage = "Steam not initialized";
            return;
        }

        if (!_steamContext.Apps.IsSubscribedApp(appId))
        {
            StatusMessage = $"App ID {appId} is not owned or not found";
            return;
        }

        if (_allGames.Any(g => g.AppId == appId))
        {
            StatusMessage = $"App ID {appId} is already in the list";
            return;
        }

        string name = _steamContext.Apps.GetAppData(appId, "name") ?? $"Game {appId}";
        string gameType = _steamContext.Apps.GetAppData(appId, "type") ?? "game";

        var game = new GameInfo
        {
            AppId = appId,
            Name = name,
            PlaytimeMinutes = 0,
            GameType = gameType,
            CoverUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg",
            HeaderImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg",
            LogoUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/logo.png",
            ImgIconUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/capsule_32x32.jpg"
        };

        _allGames.Add(game);
        SearchGames();
        AddGameAppId = string.Empty;
        StatusMessage = $"Added: {name}";
    }

    private async Task LoadCoversAsync()
    {
        var candidates = _allGames.Where(g => g.CoverImage == null && !string.IsNullOrEmpty(g.CoverUrl)).ToList();
        await Parallel.ForEachAsync(candidates, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (game, ct) =>
        {
            var image = await _imageCacheService.GetOrDownloadAsync(game.CoverUrl!);
            if (image != null)
            {
                game.CoverImage = image;
            }
        });
    }

    [RelayCommand]
    private void SearchGames()
    {
        var recentIds = _configService.RecentlyOpenedGameIds;

        IEnumerable<GameInfo> filtered = _allGames.Where(g =>
        {
            bool matchesType = g.GameType switch
            {
                "game" => ShowGames,
                "demo" => ShowDemos,
                "mod" => ShowMods,
                "junk" => ShowJunk,
                _ => ShowGames
            };

            bool matchesSearch = string.IsNullOrWhiteSpace(SearchText) ||
                g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

            return matchesType && matchesSearch;
        });

        var sorted = filtered
            .OrderByDescending(g => g.IsFavorite)
            .ThenBy(g => GetRecentIndex(g.AppId, recentIds))
            .ThenBy(g => g.Name);

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

    partial void OnShowGamesChanged(bool value) => SearchGames();
    partial void OnShowDemosChanged(bool value) => SearchGames();
    partial void OnShowModsChanged(bool value) => SearchGames();
    partial void OnShowJunkChanged(bool value) => SearchGames();

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
        var favoriteIds = new HashSet<uint>(_configService.FavoriteGameIds);
        foreach (var game in _allGames)
        {
            game.IsFavorite = favoriteIds.Contains(game.AppId);
        }
    }
}
