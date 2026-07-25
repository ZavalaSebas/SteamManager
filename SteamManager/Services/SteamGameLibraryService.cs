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
    private static readonly string LogFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "steammanager_launcher.txt");

    public SteamGameLibraryService(SteamContext steamContext)
    {
        _steamContext = steamContext;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", Config.UserAgent);
    }

    public async Task<List<GameInfo>> GetOwnedGamesAsync()
    {
        var games = new List<GameInfo>();

        try
        {
            Log("Downloading game list...");
            List<uint> appIds = await DownloadGameListAsync();
            Log($"Downloaded {appIds.Count} app IDs, checking ownership (IsInitialized={_steamContext.IsInitialized})...");

            foreach (var appId in appIds)
            {
                if (_steamContext.IsInitialized && _steamContext.Apps.IsSubscribedApp(appId))
                {
                    string name = _steamContext.Apps.GetAppData(appId, "name") ?? $"Game {appId}";

                    games.Add(new GameInfo
                    {
                        AppId = appId,
                        Name = name,
                        PlaytimeMinutes = 0,
                        CoverUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg",
                        HeaderImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg",
                        LogoUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/logo.png",
                        ImgIconUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/capsule_32x32.jpg"
                    });
                }
            }

            Log($"User owns {games.Count} games");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }

        return games;
    }

    private async Task<List<uint>> DownloadGameListAsync()
    {
        var appIds = new List<uint>();

        string xml = await _httpClient.GetStringAsync(GamesListUrl);
        Log("Got XML response");

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

        return appIds;
    }

    private static void Log(string msg)
    {
        try
        {
            System.IO.File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        }
        catch { }
    }
}
