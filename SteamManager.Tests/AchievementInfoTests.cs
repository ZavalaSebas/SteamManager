using SteamManager.Models;

namespace SteamManager.Tests;

public class AchievementInfoTests
{
    [Fact]
    public void IsUnlocked_NotifiesPropertyChanged()
    {
        var achievement = new AchievementInfo { ApiName = "Test" };
        var changed = false;

        achievement.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AchievementInfo.IsUnlocked))
                changed = true;
        };

        achievement.IsUnlocked = true;

        Assert.True(changed);
        Assert.True(achievement.IsUnlocked);
    }

    [Fact]
    public void IsSelected_NotifiesPropertyChanged()
    {
        var achievement = new AchievementInfo { ApiName = "Test" };
        var changed = false;

        achievement.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AchievementInfo.IsSelected))
                changed = true;
        };

        achievement.IsSelected = true;

        Assert.True(changed);
        Assert.True(achievement.IsSelected);
    }

    [Fact]
    public void NotifyIconChanged_RaisesPropertyChanged()
    {
        var achievement = new AchievementInfo { ApiName = "Test" };
        var changed = false;

        achievement.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AchievementInfo.Icon))
                changed = true;
        };

        achievement.NotifyIconChanged();

        Assert.True(changed);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(7, true)]
    public void IsProtected_ReturnsCorrectValue_WhenVerified(int permission, bool expectedProtected)
    {
        var achievement = new AchievementInfo { ApiName = "Test", Permission = permission, PermissionVerified = true };
        Assert.Equal(expectedProtected, achievement.IsProtected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    public void IsProtected_IsFalse_WhenNotVerified(int permission)
    {
        var achievement = new AchievementInfo { ApiName = "Test", Permission = permission, PermissionVerified = false };
        Assert.False(achievement.IsProtected);
    }

    [Fact]
    public void IsUnverified_IsTrue_WhenPermissionVerifiedIsFalse()
    {
        var achievement = new AchievementInfo { ApiName = "Test", PermissionVerified = false };
        Assert.True(achievement.IsUnverified);
    }

    [Fact]
    public void IsUnverified_IsFalse_WhenPermissionVerifiedIsTrue()
    {
        var achievement = new AchievementInfo { ApiName = "Test", PermissionVerified = true };
        Assert.False(achievement.IsUnverified);
    }

    [Fact]
    public void IsProtected_IsFalse_WhenPermissionIsZero_AndVerified()
    {
        var achievement = new AchievementInfo { ApiName = "Test", Permission = 0, PermissionVerified = true };
        Assert.False(achievement.IsProtected);
    }

    [Fact]
    public void IsProtected_IsTrue_WhenPermissionIsOne_AndVerified()
    {
        var achievement = new AchievementInfo { ApiName = "Test", Permission = 1, PermissionVerified = true };
        Assert.True(achievement.IsProtected);
    }

    [Fact]
    public void IsProtected_IsTrue_WhenPermissionIsTwo_AndVerified()
    {
        var achievement = new AchievementInfo { ApiName = "Test", Permission = 2, PermissionVerified = true };
        Assert.True(achievement.IsProtected);
    }
}
