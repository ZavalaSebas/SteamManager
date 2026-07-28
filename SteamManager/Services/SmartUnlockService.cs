using SteamManager.Steam;

namespace SteamManager.Services;

public class SmartUnlockService : ISmartUnlockService
{
    private readonly ISteamAchievements _achievements;
    private readonly ISteamStats _stats;
    private readonly Random _random = new();

    public SmartUnlockService(ISteamAchievements achievements, ISteamStats stats)
    {
        _achievements = achievements;
        _stats = stats;
    }

    public async Task<SmartUnlockResult> UnlockAchievementsAsync(
        IEnumerable<(string Id, int Permission)> achievements,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        IProgress<SmartUnlockProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var achList = achievements.ToList();
        var total = achList.Count;
        int applied = 0;
        int protectedCount = 0;
        int failed = 0;

        try
        {
            for (int i = 0; i < achList.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (id, permission) = achList[i];

                if ((permission & 3) != 0)
                {
                    protectedCount++;
                }
                else
                {
                    bool success = _achievements.SetAchievement(id, permission);
                    if (success)
                    {
                        applied++;
                    }
                    else
                    {
                        failed++;
                    }
                }

                progress?.Report(new SmartUnlockProgress(i + 1, total, applied, protectedCount, failed));

                if (i < achList.Count - 1)
                {
                    var delay = GetRandomDelay(minDelay, maxDelay);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
        finally
        {
            if (applied > 0)
            {
                _stats.StoreStats();
            }
        }

        return new SmartUnlockResult(applied, protectedCount, failed);
    }

    public async Task<SmartUnlockResult> LockAchievementsAsync(
        IEnumerable<(string Id, int Permission)> achievements,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        IProgress<SmartUnlockProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var achList = achievements.ToList();
        var total = achList.Count;
        int applied = 0;
        int protectedCount = 0;
        int failed = 0;

        try
        {
            for (int i = 0; i < achList.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (id, permission) = achList[i];

                if ((permission & 3) != 0)
                {
                    protectedCount++;
                }
                else
                {
                    bool success = _achievements.ClearAchievement(id, permission);
                    if (success)
                    {
                        applied++;
                    }
                    else
                    {
                        failed++;
                    }
                }

                progress?.Report(new SmartUnlockProgress(i + 1, total, applied, protectedCount, failed));

                if (i < achList.Count - 1)
                {
                    var delay = GetRandomDelay(minDelay, maxDelay);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
        finally
        {
            if (applied > 0)
            {
                _stats.StoreStats();
            }
        }

        return new SmartUnlockResult(applied, protectedCount, failed);
    }

    private TimeSpan GetRandomDelay(TimeSpan min, TimeSpan max)
    {
        var minMs = (int)min.TotalMilliseconds;
        var maxMs = (int)max.TotalMilliseconds;
        var randomMs = _random.Next(minMs, maxMs);
        return TimeSpan.FromMilliseconds(randomMs);
    }
}
