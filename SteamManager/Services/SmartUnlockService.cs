using SteamManager.Steam;

namespace SteamManager.Services;

public class SmartUnlockService : ISmartUnlockService
{
    private readonly SteamContext _steamContext;
    private readonly Random _random = new();

    public SmartUnlockService(SteamContext steamContext)
    {
        _steamContext = steamContext;
    }

    public async Task UnlockAchievementsAsync(
        IEnumerable<string> achievementIds,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var ids = achievementIds.ToList();
        var total = ids.Count;
        var current = 0;

        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _steamContext.Achievements.SetAchievement(id);
            _steamContext.Stats.StoreStats();

            current++;
            var percent = (int)((double)current / total * 100);
            progress?.Report(percent);

            if (current < total)
            {
                var delay = GetRandomDelay(minDelay, maxDelay);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    public async Task LockAchievementsAsync(
        IEnumerable<string> achievementIds,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var ids = achievementIds.ToList();
        var total = ids.Count;
        var current = 0;

        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _steamContext.Achievements.ClearAchievement(id);
            _steamContext.Stats.StoreStats();

            current++;
            var percent = (int)((double)current / total * 100);
            progress?.Report(percent);

            if (current < total)
            {
                var delay = GetRandomDelay(minDelay, maxDelay);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private TimeSpan GetRandomDelay(TimeSpan min, TimeSpan max)
    {
        var minMs = (int)min.TotalMilliseconds;
        var maxMs = (int)max.TotalMilliseconds;
        var randomMs = _random.Next(minMs, maxMs);
        return TimeSpan.FromMilliseconds(randomMs);
    }
}
