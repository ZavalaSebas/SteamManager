namespace SteamManager.Services;

/// <summary>
/// Interface for unlocking achievements with anti-detection delays.
/// </summary>
public interface ISmartUnlockService
{
    /// <summary>
    /// Unlocks a set of achievements with random delays between each unlock.
    /// </summary>
    Task UnlockAchievementsAsync(
        IEnumerable<string> achievementIds,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks a set of achievements with random delays between each lock.
    /// </summary>
    Task LockAchievementsAsync(
        IEnumerable<string> achievementIds,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
