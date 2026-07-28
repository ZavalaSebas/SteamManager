using System.IO;
using System.Net.Http;
using System.Xml;
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
        List<uint> appIds = await DownloadGameListAsync();
        FileLogger.Log($"Total appIds downloaded: {appIds.Count}");

        FileLogger.LogSection("CHECKING OWNERSHIP");
        int checkedCount = 0;
        int owned = 0;
        int errors = 0;
        int lastLogged = 0;

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
                        FileLogger.Log($"  Owned: {appId} - {name}");
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
                FileLogger.Log($"Progress: checked {checkedCount}/{appIds.Count}, owned {owned}, errors {errors}");
                lastLogged = checkedCount;
            }
        }

        FileLogger.LogSection("RESULTS");
        FileLogger.Log($"Total checked: {checkedCount}");
        FileLogger.Log($"Total owned: {owned}");
        FileLogger.Log($"Total errors: {errors}");
        FileLogger.Log($"Log file: {FileLogger.GetLastLogPath()}");

        return games;
    }

    private async Task<List<uint>> DownloadGameListAsync()
    {
        var appIds = new List<uint>();

        FileLogger.Log("Downloading from gib.me/sam/games.xml");

        string xml = await _httpClient.GetStringAsync(GamesListUrl);
        FileLogger.Log($"Downloaded XML length: {xml.Length} chars");

        using var stringReader = new StringReader(xml);
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null
        };

        using var xmlReader = XmlReader.Create(stringReader, settings);

        while (xmlReader.Read())
        {
            if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "game")
            {
                if (uint.TryParse(xmlReader.ReadElementContentAsString(), out uint appId) && appId > 0)
                {
                    appIds.Add(appId);
                }
            }
        }

        FileLogger.Log($"Parsed {appIds.Count} appIds from XML");

        return appIds;
    }
}
