namespace SteamManager.Services;

/// <summary>
/// Interface for retrieving the user's Steam game library.
/// </summary>
public interface IGameLibraryService
{
    /// <summary>
    /// Gets all games owned by the current Steam user.
    /// </summary>
    Task<List<Models.GameInfo>> GetOwnedGamesAsync();
}
