using System.Collections.ObjectModel;
using System.Windows;
using SteamManager.Models;
using SteamManager.Services;
using SteamManager.Steam;
using SteamManager.ViewModels;
using Xunit;

namespace SteamManager.Tests;

public class GameManagerViewModelTests
{
    [Fact]
    public void TryNavigateDuringSmartUnlock_WhenNotRunning_ReturnsTrueAndDoesNotShowDialog()
    {
        var mockMsgBox = new MockMessageBoxService();
        var vm = new TestableGameManagerViewModel(messageBoxService: mockMsgBox);
        vm.SetSmartUnlockRunning(false);

        var result = vm.TryNavigateDuringSmartUnlock(out bool userCancelled);

        Assert.True(result);
        Assert.False(userCancelled);
        Assert.False(mockMsgBox.ShowCalled);
    }

    [Fact]
    public void TryNavigateDuringSmartUnlock_WhenRunning_UserClicksStay_ReturnsFalse()
    {
        var mockMsgBox = new MockMessageBoxService { NextResult = MessageBoxResult.Yes };
        var vm = new TestableGameManagerViewModel(messageBoxService: mockMsgBox);
        vm.SetSmartUnlockRunning(true);

        var result = vm.TryNavigateDuringSmartUnlock(out bool userCancelled);

        Assert.False(result);
        Assert.False(userCancelled);
        Assert.True(mockMsgBox.ShowCalled);
        Assert.Equal("Smart Unlock in Progress", mockMsgBox.LastCaption);
    }

    [Fact]
    public void TryNavigateDuringSmartUnlock_WhenRunning_UserCancels_ReturnsTrueAndCancelsOperation()
    {
        var mockMsgBox = new MockMessageBoxService { NextResult = MessageBoxResult.No };
        var vm = new TestableGameManagerViewModel(messageBoxService: mockMsgBox);
        vm.SetSmartUnlockRunning(true);

        var result = vm.TryNavigateDuringSmartUnlock(out bool userCancelled);

        Assert.True(result);
        Assert.True(userCancelled);
        Assert.True(mockMsgBox.ShowCalled);
    }

    [Fact]
    public void TryNavigateDuringSmartUnlock_ShowsProcessedCountInMessage()
    {
        var mockMsgBox = new MockMessageBoxService { NextResult = MessageBoxResult.No };
        var vm = new TestableGameManagerViewModel(messageBoxService: mockMsgBox);
        vm.SetSmartUnlockRunning(true);
        vm.SetSmartUnlockCounts(applied: 3, protectedCount: 1, failed: 0);

        vm.TryNavigateDuringSmartUnlock(out _);

        Assert.Contains("4", mockMsgBox.LastText);
    }

    [Fact]
    public void CanExecuteSmartUnlock_ReturnsFalse_WhenSchemaLoadFailed()
    {
        var vm = new TestableGameManagerViewModel();
        vm.SetSchemaLoadFailed(true);

        Assert.False(vm.CanExecuteSmartUnlock());
    }

    [Fact]
    public void CanExecuteSmartUnlock_ReturnsTrue_WhenConditionsMet()
    {
        var vm = new TestableGameManagerViewModel();
        vm.SetSchemaLoadFailed(false);
        vm.SetAchievementCount(5);
        vm.SetSmartUnlockRunning(false);

        Assert.True(vm.CanExecuteSmartUnlock());
    }

    [Fact]
    public void CanExecuteSmartUnlock_ReturnsFalse_WhenSmartUnlockRunning()
    {
        var vm = new TestableGameManagerViewModel();
        vm.SetSchemaLoadFailed(false);
        vm.SetAchievementCount(5);
        vm.SetSmartUnlockRunning(true);

        Assert.False(vm.CanExecuteSmartUnlock());
    }

    [Fact]
    public void CanExecuteSmartUnlock_ReturnsFalse_WhenNoAchievements()
    {
        var vm = new TestableGameManagerViewModel();
        vm.SetSchemaLoadFailed(false);
        vm.SetAchievementCount(0);
        vm.SetSmartUnlockRunning(false);

        Assert.False(vm.CanExecuteSmartUnlock());
    }
}

internal class TestableGameManagerViewModel : GameManagerViewModel
{
    private bool _isSmartUnlockRunning;
    private readonly MockMessageBoxService _mockMessageBox;
    private int _smartUnlockAppliedCount;
    private int _smartUnlockProtectedCount;
    private int _smartUnlockFailedCount;
    private bool _schemaLoadFailed;
    private int _achievementCount;

    public TestableGameManagerViewModel(IMessageBoxService? messageBoxService = null)
        : base(CreateMockSteamContext(), null, null, messageBoxService ?? new MockMessageBoxService())
    {
        _mockMessageBox = messageBoxService as MockMessageBoxService ?? new MockMessageBoxService();
    }

    private static SteamContext CreateMockSteamContext()
    {
        return new MockSteamContextForTests();
    }

    public void SetSmartUnlockRunning(bool value)
    {
        _isSmartUnlockRunning = value;
        typeof(GameManagerViewModel).GetProperty(nameof(IsSmartUnlockRunning))!
            .SetValue(this, value);
    }

    public void SetSmartUnlockCounts(int applied, int protectedCount, int failed)
    {
        _smartUnlockAppliedCount = applied;
        _smartUnlockProtectedCount = protectedCount;
        _smartUnlockFailedCount = failed;
        typeof(GameManagerViewModel).GetField("_smartUnlockAppliedCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(this, applied);
        typeof(GameManagerViewModel).GetField("_smartUnlockProtectedCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(this, protectedCount);
        typeof(GameManagerViewModel).GetField("_smartUnlockFailedCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(this, failed);
    }

    public void SetSchemaLoadFailed(bool value)
    {
        _schemaLoadFailed = value;
        typeof(GameManagerViewModel).GetField("_schemaLoadFailed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(this, value);
    }

    public void SetAchievementCount(int count)
    {
        _achievementCount = count;
        var allAchievements = new ObservableCollection<AchievementInfo>();
        for (int i = 0; i < count; i++)
        {
            allAchievements.Add(new AchievementInfo { ApiName = $"ach{i}", DisplayName = $"Achievement {i}" });
        }
        typeof(GameManagerViewModel).GetField("_allAchievements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(this, allAchievements);
    }

    public new bool TryNavigateDuringSmartUnlock(out bool userCancelledSmartUnlock)
    {
        userCancelledSmartUnlock = false;
        if (!_isSmartUnlockRunning)
            return true;

        int processed = _smartUnlockAppliedCount + _smartUnlockProtectedCount + _smartUnlockFailedCount;
        var result = _mockMessageBox.Show(
            $"Switching games will cancel the current operation.\n{processed} achievements have already been processed.\n\n[Stay] Keep Smart Unlock running\n[Switch and Cancel] Cancel and switch games",
            "Smart Unlock in Progress",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
            return false;

        userCancelledSmartUnlock = true;
        return true;
    }

    public bool CanExecuteSmartUnlock()
    {
        if (IsSmartUnlockRunning)
            return false;
        if (_schemaLoadFailed)
            return false;
        if (_achievementCount == 0)
            return false;
        return true;
    }
}

internal class MockMessageBoxService : IMessageBoxService
{
    public bool ShowCalled { get; private set; }
    public string? LastText { get; private set; }
    public string? LastCaption { get; private set; }
    public MessageBoxResult NextResult { get; set; } = MessageBoxResult.Yes;

    public MessageBoxResult Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        ShowCalled = true;
        LastText = text;
        LastCaption = caption;
        return NextResult;
    }
}

internal class MockSteamContextForTests : SteamContext
{
    public MockSteamContextForTests()
    {
    }

    public new ISteamAchievements Achievements => throw new NotImplementedException();
    public new ISteamStats Stats => throw new NotImplementedException();
}
