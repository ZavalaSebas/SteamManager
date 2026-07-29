using System.IO;
using System.Net.Http;
using System.Xml.XPath;
using SteamManager.Models;
using SteamManager.Steam;

namespace SteamManager.Services;

public class SteamGameLibraryService : IGameLibraryService
{
    private readonly SteamContext _steamContext;
    private readonly HttpClient _httpClient;
    private const string GamesListUrl = "https://gib.me/sam/games.xml";

    public SteamGameLibraryService(SteamContext steamContext)
    {
        _steamContext = steamContext;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", Config.UserAgent);
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
