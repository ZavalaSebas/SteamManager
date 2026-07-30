using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.XPath;
using SteamManager.Models;
using SteamManager.Steam;

namespace SteamManager.Services;

public class SteamGameLibraryService : IGameLibraryService
{
    private readonly SteamContext _steamContext;
    private readonly HttpClient _httpClient;
    private const string GamesListUrl = "https://gib.me/sam/games.xml";
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SteamManager", "cache");
    private static readonly string CachePath = Path.Combine(CacheDir, "games.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SteamGameLibraryService(SteamContext steamContext)
    {
        _steamContext = steamContext;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", Config.UserAgent);
    }

    public Task<List<GameInfo>> GetCachedGamesAsync()
    {
        try
        {
            if (!File.Exists(CachePath))
                return Task.FromResult(new List<GameInfo>());

            var json = File.ReadAllText(CachePath);
            var cached = JsonSerializer.Deserialize<List<CachedGameInfo>>(json, JsonOptions);
            if (cached == null || cached.Count == 0)
                return Task.FromResult(new List<GameInfo>());

            var games = cached.Select(c => new GameInfo
            {
                AppId = c.AppId,
                Name = c.Name ?? $"Game {c.AppId}",
                GameType = c.GameType ?? "game",
                CoverUrl = c.CoverUrl ?? $"https://steamcdn-a.akamaihd.net/steam/apps/{c.AppId}/header.jpg",
                HeaderImageUrl = c.CoverUrl ?? $"https://steamcdn-a.akamaihd.net/steam/apps/{c.AppId}/header.jpg",
                LogoUrl = c.LogoUrl ?? $"https://steamcdn-a.akamaihd.net/steam/apps/{c.AppId}/logo.png",
                ImgIconUrl = c.ImgIconUrl ?? $"https://steamcdn-a.akamaihd.net/steam/apps/{c.AppId}/capsule_32x32.jpg"
            }).ToList();

            return Task.FromResult(games);
        }
        catch
        {
            return Task.FromResult(new List<GameInfo>());
        }
    }

    public Task SaveGamesCacheAsync(List<GameInfo> games)
    {
        try
        {
            var cached = games.Select(g => new CachedGameInfo
            {
                AppId = g.AppId,
                Name = g.Name,
                GameType = g.GameType,
                CoverUrl = g.CoverUrl,
                LogoUrl = g.LogoUrl,
                ImgIconUrl = g.ImgIconUrl
            }).ToList();

            Directory.CreateDirectory(CacheDir);
            var json = JsonSerializer.Serialize(cached, JsonOptions);
            File.WriteAllText(CachePath, json);
        }
        catch { }
        return Task.CompletedTask;
    }

    private record CachedGameInfo
    {
        public uint AppId { get; init; }
        public string? Name { get; init; }
        public string? GameType { get; init; }
        public string? CoverUrl { get; init; }
        public string? LogoUrl { get; init; }
        public string? ImgIconUrl { get; init; }
    }

    public async Task<List<GameInfo>> GetOwnedGamesAsync()
    {
        var games = new List<GameInfo>();

        FileLogger.LogSection("DOWNLOADING GAME LIST");
        FileLogger.Log($"Steam ID: {_steamContext.SteamId}");
        List<uint> appIds = await DownloadGameListAsync();
        FileLogger.Log($"Total appIds downloaded: {appIds.Count}");

        FileLogger.LogSection("CHECKING OWNERSHIP");
        int checkedCount = 0;
        int owned = 0;
        int errors = 0;
        int lastLogged = 0;
        int ownGames = 0;
        int familySharedGames = 0;

        foreach (var appId in appIds)
        {
            checkedCount++;

            try
            {
                if (!_steamContext.IsInitialized)
                {
                    FileLogger.Log($"ERROR: Steam not initialized at app {appId}");
                    errors++;
                    continue;
                }

                bool isOwned = _steamContext.Apps.IsSubscribedApp(appId);

                if (isOwned)
                {
                    owned++;
                    bool isFamilyShared = _steamContext.Apps.IsSubscribedFromFamilySharing(appId);
                    if (isFamilyShared)
                        familySharedGames++;
                    else
                        ownGames++;

                    string name = _steamContext.Apps.GetAppData(appId, "name") ?? $"Game {appId}";

                    games.Add(new GameInfo
                    {
                        AppId = appId,
                        Name = name,
                        PlaytimeMinutes = 0,
                        GameType = _steamContext.Apps.GetAppData(appId, "type") ?? "game",
                        CoverUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg",
                        HeaderImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg",
                        LogoUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/logo.png",
                        ImgIconUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/capsule_32x32.jpg"
                    });

                    if (owned <= 10 || owned % 50 == 0)
                    {
                        FileLogger.Log($"  Owned: {appId} - {name}" + (isFamilyShared ? " [FAMILY SHARED]" : ""));
                    }
                }
            }
            catch (Exception ex)
            {
                errors++;
                FileLogger.Log($"EXCEPTION at appId {appId}: {ex.Message}");
            }

            if (checkedCount - lastLogged >= 5000)
            {
                FileLogger.Log($"Progress: checked {checkedCount}/{appIds.Count}, owned {owned}, own {ownGames}, family {familySharedGames}, errors {errors}");
                lastLogged = checkedCount;
            }
        }

        FileLogger.LogSection("RESULTS");
        FileLogger.Log($"Total checked: {checkedCount}");
        FileLogger.Log($"Total owned (all): {owned}");
        FileLogger.Log($"  Own games (not family shared): {ownGames}");
        FileLogger.Log($"  Family shared games: {familySharedGames}");
        FileLogger.Log($"Total errors: {errors}");

        return games;
    }

    private async Task<List<uint>> DownloadGameListAsync()
    {
        var appIds = new List<uint>();

        string xml = await _httpClient.GetStringAsync(GamesListUrl);

        using var stringReader = new StringReader(xml);
        var document = new XPathDocument(stringReader);
        var navigator = document.CreateNavigator();
        var nodes = navigator.Select("/games/game");

        while (nodes.MoveNext())
        {
            if (uint.TryParse(nodes.Current.Value, out uint appId) && appId > 0)
            {
                appIds.Add(appId);
            }
        }

        return appIds;
    }
}
