using SteamManager.Services;

namespace SteamManager.Tests;

public class GameStatsTests
{
    [Fact]
    public void GetStatsForGame_ReturnsStatsForKnownGame()
    {
        var stats = GameStats.GetStatsForGame(440);

        Assert.NotEmpty(stats);
        Assert.Contains("miles", stats);
    }

    [Fact]
    public void GetStatsForGame_ReturnsEmptyForUnknownGame()
    {
        var stats = GameStats.GetStatsForGame(999999);

        Assert.Empty(stats);
    }

    [Fact]
    public void HasStats_ReturnsTrueForKnownGame()
    {
        Assert.True(GameStats.HasStats(440));
        Assert.True(GameStats.HasStats(570));
        Assert.True(GameStats.HasStats(730));
    }

    [Fact]
    public void HasStats_ReturnsFalseForUnknownGame()
    {
        Assert.False(GameStats.HasStats(999999));
    }

    [Fact]
    public void PopularGameStats_ContainsExpectedGames()
    {
        Assert.True(GameStats.PopularGameStats.ContainsKey(440));  // TF2
        Assert.True(GameStats.PopularGameStats.ContainsKey(570));  // Dota 2
        Assert.True(GameStats.PopularGameStats.ContainsKey(730));  // CS2
        Assert.True(GameStats.PopularGameStats.ContainsKey(252490)); // Rust
    }
}
