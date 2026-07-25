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
}
