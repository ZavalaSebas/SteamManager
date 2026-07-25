namespace SteamManager.Services;

/// <summary>
/// Interface for persisting user settings.
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// Gets the list of favorite game AppIds.
    /// </summary>
    List<uint> FavoriteGameIds { get; }

    /// <summary>
    /// Adds a game to favorites.
    /// </summary>
    void AddFavorite(uint appId);

    /// <summary>
    /// Removes a game from favorites.
    /// </summary>
    void RemoveFavorite(uint appId);

    /// <summary>
    /// Gets whether a game is favorited.
    /// </summary>
    bool IsFavorite(uint appId);

    /// <summary>
    /// Gets the last selected game AppId.
    /// </summary>
    uint? LastSelectedGameId { get; set; }

    /// <summary>
    /// Gets the minimum unlock delay in seconds.
    /// </summary>
    int MinUnlockDelaySeconds { get; set; }

    /// <summary>
    /// Gets the maximum unlock delay in seconds.
    /// </summary>
    int MaxUnlockDelaySeconds { get; set; }

    /// <summary>
    /// Gets the theme preference.
    /// </summary>
    string Theme { get; set; }

    /// <summary>
    /// Saves the configuration to disk.
    /// </summary>
    void Save();

    /// <summary>
    /// Loads the configuration from disk.
    /// </summary>
    void Load();
}
