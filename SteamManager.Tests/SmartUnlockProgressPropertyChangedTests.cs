using System.Collections.ObjectModel;
using SteamManager.Models;
using SteamManager.Services;
using SteamManager.Steam;
using SteamManager.ViewModels;
using Xunit;

namespace SteamManager.Tests;

public class SmartUnlockProgressPropertyChangedTests
{
    [Fact]
    public async Task ExecuteSmartUnlockAsync_FiresPropertyChanged_ForProgressPercent_AndAppliedCount()
    {
        var fakeService = new FakeSmartUnlockServiceWithProgressTracking();
        var vm = CreateTestableVM(fakeService);
        SetupVM(vm, 3);

        var firedProperties = new List<string>();
        vm.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName is nameof(GameManagerViewModel.SmartUnlockProgressPercent)
                or nameof(GameManagerViewModel.SmartUnlockAppliedCount)
                or nameof(GameManagerViewModel.SmartUnlockProcessed))
            {
                firedProperties.Add(e.PropertyName);
            }
        };

        await InvokeExecuteSmartUnlockAsync(vm, 0, 0, true);

        Assert.Contains(nameof(GameManagerViewModel.SmartUnlockAppliedCount), firedProperties);
        Assert.Contains(nameof(GameManagerViewModel.SmartUnlockProgressPercent), firedProperties);
    }

    [Fact]
    public async Task ExecuteSmartUnlockAsync_SetsProgressPercent_To100_OnCompletion()
    {
        var fakeService = new FakeSmartUnlockServiceWithProgressTracking();
        var vm = CreateTestableVM(fakeService);
        SetupVM(vm, 3);

        await InvokeExecuteSmartUnlockAsync(vm, 0, 0, true);

        Assert.Equal(100, vm.SmartUnlockProgressPercent);
    }

    [Fact]
    public async Task ExecuteSmartUnlockAsync_ProgressPercent_NeverStaysAtZero_DuringExecution()
    {
        var fakeService = new FakeSmartUnlockServiceWithProgressTracking();
        var vm = CreateTestableVM(fakeService);
        SetupVM(vm, 3);

        var progressValues = new List<int>();
        vm.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(GameManagerViewModel.SmartUnlockProgressPercent))
            {
                progressValues.Add(vm.SmartUnlockProgressPercent);
            }
        };

        await InvokeExecuteSmartUnlockAsync(vm, 0, 0, true);

        Assert.NotEmpty(progressValues);
        Assert.Contains(progressValues, v => v > 0);
    }

    [Fact]
    public async Task ExecuteSmartUnlockAsync_AppliedCount_ReachesCorrectTotal_OnCompletion()
    {
        var fakeService = new FakeSmartUnlockServiceWithProgressTracking();
        var vm = CreateTestableVM(fakeService);
        SetupVM(vm, 3);

        await InvokeExecuteSmartUnlockAsync(vm, 0, 0, true);

        Assert.Equal(3, vm.SmartUnlockAppliedCount);
    }

    private static GameManagerViewModel CreateTestableVM(ISmartUnlockService fakeService)
    {
        var steamContext = new FakeSteamContext();
        var vm = new GameManagerViewModel(steamContext, null, null, new MockMessageBoxService(), fakeService);
        return vm;
    }

    private static void SetupVM(GameManagerViewModel vm, int count)
    {
        var achievements = new ObservableCollection<AchievementInfo>();
        for (int i = 0; i < count; i++)
        {
            achievements.Add(new AchievementInfo
            {
                ApiName = $"ach{i}",
                DisplayName = $"Achievement {i}",
                Permission = 0,
                IsUnlocked = false
            });
        }

        var allAchFi = typeof(GameManagerViewModel).GetField("_allAchievements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        allAchFi.SetValue(vm, achievements);

        var selectedGameProp = typeof(GameManagerViewModel).GetProperty(nameof(GameManagerViewModel.SelectedGame))!;
        selectedGameProp.SetValue(vm, new GameInfo { AppId = 480, Name = "TestGame" });

        var schemaLoadFailedFi = typeof(GameManagerViewModel).GetField("_schemaLoadFailed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        schemaLoadFailedFi.SetValue(vm, false);
    }

    private static async Task InvokeExecuteSmartUnlockAsync(GameManagerViewModel vm, int minDelayMs, int maxDelayMs, bool showOverlay)
    {
        var method = typeof(GameManagerViewModel).GetMethod(
            "ExecuteSmartUnlockAsync",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!;
        await (Task)method.Invoke(vm, new object[] { minDelayMs, maxDelayMs, showOverlay })!;
    }
}

internal class FakeSmartUnlockServiceWithProgressTracking : ISmartUnlockService
{
    public Task<SmartUnlockResult> UnlockAchievementsAsync(
        IEnumerable<(string Id, int Permission)> achievements,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        IProgress<SmartUnlockProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var achList = achievements.ToList();
        int total = achList.Count;
        int applied = 0;
        int protectedCount = 0;
        int failed = 0;

        for (int i = 0; i < achList.Count; i++)
        {
            applied++;
            progress?.Report(new SmartUnlockProgress(i + 1, total, applied, protectedCount, failed));
        }

        return Task.FromResult(new SmartUnlockResult(applied, protectedCount, failed));
    }

    public Task<SmartUnlockResult> LockAchievementsAsync(
        IEnumerable<(string Id, int Permission)> achievements,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        IProgress<SmartUnlockProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var achList = achievements.ToList();
        int total = achList.Count;
        int applied = 0;
        int protectedCount = 0;
        int failed = 0;

        for (int i = 0; i < achList.Count; i++)
        {
            applied++;
            progress?.Report(new SmartUnlockProgress(i + 1, total, applied, protectedCount, failed));
        }

        return Task.FromResult(new SmartUnlockResult(applied, protectedCount, failed));
    }
}

internal class FakeSteamContext : SteamContext
{
    public FakeSteamContext() { }

    public new ISteamAchievements Achievements => new FakeSteamAchievements();
    public new ISteamStats Stats => new FakeSteamStats();
}

internal class FakeSteamAchievements : ISteamAchievements
{
    public bool SetAchievement(string name, int permission) => true;
    public bool ClearAchievement(string name, int permission) => true;
    public IEnumerable<AchievementInfo> GetAllAchievements() => [];
}

internal class FakeSteamStats : ISteamStats
{
    public bool StoreStats() => true;
    public bool ResetAllStats(bool resetAchievements) => true;
    public bool GetStat(string name, out int value) { value = 0; return true; }
    public bool GetStat(string name, out float value) { value = 0; return true; }
    public bool SetStat(string name, int value) => true;
    public bool SetStat(string name, float value) => true;
    public bool RequestCurrentStats() => true;
}
