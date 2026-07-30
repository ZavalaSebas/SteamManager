namespace SteamManager.Services;

public interface IGameLibraryService
{
    Task<List<Models.GameInfo>> GetOwnedGamesAsync();
    Task<List<Models.GameInfo>> GetCachedGamesAsync();
    Task SaveGamesCacheAsync(List<Models.GameInfo> games);
}
