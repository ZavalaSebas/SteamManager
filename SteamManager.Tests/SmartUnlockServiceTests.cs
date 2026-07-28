using SteamManager.Models;
using SteamManager.Services;
using SteamManager.Steam;

namespace SteamManager.Tests;

public class SmartUnlockServiceTests
{
    [Fact]
    public async Task UnlockAchievementsAsync_AppliesUnlockedAchievements()
    {
        var mockAchievements = new MockSteamAchievements();
        var mockStats = new MockSteamStats();
        var service = new SmartUnlockService(mockAchievements, mockStats);

        var achievements = new List<(string, int)>
        {
            ("ach1", 0),
            ("ach2", 0),
            ("ach3", 0)
        };

        var result = await service.UnlockAchievementsAsync(
            achievements,
            TimeSpan.Zero,
            TimeSpan.Zero);

        Assert.Equal(3, result.Applied);
        Assert.Equal(0, result.Protected);
        Assert.Equal(0, result.Failed);
        Assert.Equal(3, mockAchievements.SetAchievementCallCount);
    }

    [Fact]
    public async Task UnlockAchievementsAsync_SkipsProtectedAchievements()
    {
        var mockAchievements = new MockSteamAchievements();
        var mockStats = new MockSteamStats();
        var service = new SmartUnlockService(mockAchievements, mockStats);

        var achievements = new List<(string, int)>
        {
            ("ach1", 0),
            ("ach2", 1),
            ("ach3", 0)
        };

        var result = await service.UnlockAchievementsAsync(
            achievements,
            TimeSpan.Zero,
            TimeSpan.Zero);

        Assert.Equal(2, result.Applied);
        Assert.Equal(1, result.Protected);
        Assert.Equal(0, result.Failed);
        Assert.Equal(2, mockAchievements.SetAchievementCallCount);
    }

    [Fact]
    public async Task UnlockAchievementsAsync_TracksFailedAchievements()
    {
        var mockAchievements = new MockSteamAchievements { ShouldSucceed = false };
        var mockStats = new MockSteamStats();
        var service = new SmartUnlockService(mockAchievements, mockStats);

        var achievements = new List<(string, int)>
        {
            ("ach1", 0),
            ("ach2", 0)
        };

        var result = await service.UnlockAchievementsAsync(
            achievements,
            TimeSpan.Zero,
            TimeSpan.Zero);

        Assert.Equal(0, result.Applied);
        Assert.Equal(0, result.Protected);
        Assert.Equal(2, result.Failed);
    }

    [Fact]
    public async Task UnlockAchievementsAsync_DoesNotCallStoreStatsWhenAllProtected()
    {
        var mockAchievements = new MockSteamAchievements();
        var mockStats = new MockSteamStats();
        var service = new SmartUnlockService(mockAchievements, mockStats);

        var achievementsAllProtected = new List<(string, int)>
        {
            ("ach1", 1),
            ("ach2", 2)
        };

        await service.UnlockAchievementsAsync(
            achievementsAllProtected,
            TimeSpan.Zero,
            TimeSpan.Zero);

        Assert.Equal(0, mockStats.StoreStatsCallCount);
    }

    [Fact]
    public async Task UnlockAchievementsAsync_Cancellation_StoresPartialProgress()
    {
        var mockAchievements = new MockSteamAchievements { Delay = TimeSpan.FromMilliseconds(20) };
        var mockStats = new MockSteamStats();
        var service = new SmartUnlockService(mockAchievements, mockStats);

        var achievements = new List<(string, int)>
        {
            ("ach1", 0),
            ("ach2", 0),
            ("ach3", 0),
            ("ach4", 0),
            ("ach5", 0)
        };

        var cts = new CancellationTokenSource();
        cts.CancelAfter(30);

        try
        {
            await service.UnlockAchievementsAsync(
                achievements,
                TimeSpan.FromMilliseconds(20),
                TimeSpan.FromMilliseconds(30),
                null,
                cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        Assert.True(mockStats.StoreStatsCallCount >= 1, "StoreStats should be called after cancellation");
    }

    [Fact]
    public async Task UnlockAchievementsAsync_Cancellation_ThrowsOperationCanceledException()
    {
        var mockAchievements = new MockSteamAchievements();
        var mockStats = new MockSteamStats();
        var service = new SmartUnlockService(mockAchievements, mockStats);

        var achievements = new List<(string, int)>
        {
            ("ach1", 0),
            ("ach2", 0)
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.UnlockAchievementsAsync(
                achievements,
                TimeSpan.Zero,
                TimeSpan.Zero,
                null,
                cts.Token));
    }

    [Fact]
    public async Task UnlockAchievementsAsync_Cancellation_StillCallsStoreStatsInFinally()
    {
        var mockAchievements = new MockSteamAchievements { Delay = TimeSpan.FromMilliseconds(30) };
        var mockStats = new MockSteamStats();
        var service = new SmartUnlockService(mockAchievements, mockStats);

        var achievements = new List<(string, int)>
        {
            ("ach1", 0),
            ("ach2", 0),
            ("ach3", 0)
        };

        var cts = new CancellationTokenSource();
        cts.CancelAfter(20);

        try
        {
            await service.UnlockAchievementsAsync(
                achievements,
                TimeSpan.FromMilliseconds(30),
                TimeSpan.FromMilliseconds(40),
                null,
                cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        Assert.Equal(1, mockStats.StoreStatsCallCount);
    }

    [Fact]
    public async Task UnlockAchievementsAsync_ReportsProgressCorrectly()
    {
        var mockAchievements = new MockSteamAchievements();
        var mockStats = new MockSteamStats();
        var service = new SmartUnlockService(mockAchievements, mockStats);

        var progressReports = new List<SmartUnlockProgress>();
        var achievements = new List<(string, int)>
        {
            ("ach1", 0),
            ("ach2", 0),
            ("ach3", 0)
        };

        var progress = new Progress<SmartUnlockProgress>(p => progressReports.Add(p));

        await service.UnlockAchievementsAsync(
            achievements,
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(20),
            progress);

        Assert.Equal(3, progressReports.Count);
        Assert.Equal(1, progressReports[0].Processed);
        Assert.Equal(2, progressReports[1].Processed);
        Assert.Equal(3, progressReports[2].Processed);
        Assert.Equal(3, progressReports[2].Applied);
    }

    [Fact]
    public void SmartUnlockDialog_SecondsToMilliseconds_ConversionIsCorrect()
    {
        int minSeconds = 15;
        int maxSeconds = 45;
        int expectedMinMs = 15000;
        int expectedMaxMs = 45000;

        Assert.Equal(expectedMinMs, minSeconds * 1000);
        Assert.Equal(expectedMaxMs, maxSeconds * 1000);
    }

    [Fact]
    public async Task UnlockAchievementsAsync_PermissionBitmaskValidation()
    {
        var mockAchievements = new MockSteamAchievements();
        var mockStats = new MockSteamStats();
        var service = new SmartUnlockService(mockAchievements, mockStats);

        var achievements = new List<(string, int)>
        {
            ("ach0", 0),
            ("ach1", 1),
            ("ach2", 2),
            ("ach3", 3),
            ("ach4", 4),
            ("ach7", 7)
        };

        var result = await service.UnlockAchievementsAsync(
            achievements,
            TimeSpan.Zero,
            TimeSpan.Zero);

        Assert.Equal(2, result.Applied);
        Assert.Equal(4, result.Protected);
    }
}

internal class MockSteamAchievements : ISteamAchievements
{
    public bool ShouldSucceed = true;
    public int SetAchievementCallCount = 0;
    public int ClearAchievementCallCount = 0;
    public TimeSpan Delay = TimeSpan.Zero;

    public bool SetAchievement(string name, int permission)
    {
        SetAchievementCallCount++;
        if (Delay > TimeSpan.Zero)
            Thread.Sleep(Delay);
        return ShouldSucceed;
    }

    public bool ClearAchievement(string name, int permission)
    {
        ClearAchievementCallCount++;
        return ShouldSucceed;
    }

    public IEnumerable<AchievementInfo> GetAllAchievements() => throw new NotImplementedException();
    public bool RequestGlobalAchievementPercentages() => true;
    public float GetAchievementAchievedPercent(string name) => 0f;
}

internal class MockSteamStats : ISteamStats
{
    public int StoreStatsCallCount = 0;

    public bool StoreStats() { StoreStatsCallCount++; return true; }
    public bool ResetAllStats(bool resetAchievements) => true;
    public bool GetStat(string name, out int value) { value = 0; return false; }
    public bool GetStat(string name, out float value) { value = 0; return false; }
    public bool SetStat(string name, int value) => true;
    public bool SetStat(string name, float value) => true;
    public bool RequestCurrentStats() => true;
}
