using SteamManager.Models;
using SteamManager.Steam;

namespace SteamManager.Services;

/// <summary>
/// Implementation of IGameLibraryService that reads from the Steam API.
/// </summary>
public class SteamGameLibraryService : IGameLibraryService
{
    private readonly SteamContext _steamContext;

    public SteamGameLibraryService(SteamContext steamContext)
    {
        _steamContext = steamContext;
    }

    public Task<List<GameInfo>> GetOwnedGamesAsync()
    {
        // For now, return a placeholder list.
        // Full implementation will use SteamApps to enumerate owned games.
        var games = new List<GameInfo>
        {
            new() { AppId = Config.SpacewarAppId, Name = "Spacewar (Test)", PlaytimeMinutes = 0 }
        };

        return Task.FromResult(games);
    }
}
