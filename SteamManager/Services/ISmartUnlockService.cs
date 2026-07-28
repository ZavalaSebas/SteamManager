namespace SteamManager.Services;

public record SmartUnlockResult(int Applied, int Protected, int Failed);

public record SmartUnlockProgress(int Processed, int Total, int Applied, int Protected, int Failed);

/// <summary>
/// Interface for unlocking achievements with anti-detection delays.
/// </summary>
public interface ISmartUnlockService
{
    /// <summary>
    /// Unlocks a set of achievements with random delays between each unlock.
    /// Skips achievements where (permission &amp; 3) != 0.
    /// Always calls StoreStats() in finally if applied > 0, even on cancellation.
    /// Returns a result with counts of applied, protected, and failed operations.
    /// </summary>
    Task<SmartUnlockResult> UnlockAchievementsAsync(
        IEnumerable<(string Id, int Permission)> achievements,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        IProgress<SmartUnlockProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks a set of achievements with random delays between each lock.
    /// Skips achievements where (permission &amp; 3) != 0.
    /// Always calls StoreStats() in finally if applied > 0, even on cancellation.
    /// Returns a result with counts of applied, protected, and failed operations.
    /// </summary>
    Task<SmartUnlockResult> LockAchievementsAsync(
        IEnumerable<(string Id, int Permission)> achievements,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        IProgress<SmartUnlockProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
