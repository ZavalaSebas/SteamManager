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

        List<uint> appIds = await DownloadGameListAsync();

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

        return games;
    }

    private async Task<List<uint>> DownloadGameListAsync()
    {
        var appIds = new List<uint>();

        string xml = await _httpClient.GetStringAsync(GamesListUrl);

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
}
