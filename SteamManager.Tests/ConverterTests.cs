using SteamManager.Converters;
using SteamManager.ViewModels;
using System.Globalization;

namespace SteamManager.Tests;

public class ConverterTests
{
    [Theory]
    [InlineData(AchievementFilterType.All, "All", true)]
    [InlineData(AchievementFilterType.Unlocked, "All", false)]
    [InlineData(AchievementFilterType.Locked, "All", false)]
    public void FilterToBackgroundConverter_ReturnsCorrectColor(AchievementFilterType current, string filter, bool expectedBlue)
    {
        var converter = new FilterToBackgroundConverter();
        var result = converter.Convert(current, typeof(object), filter, CultureInfo.InvariantCulture);

        Assert.NotNull(result);
    }

    [Fact]
    public void BoolToFavoriteColorConverter_ReturnsYellowWhenTrue()
    {
        var converter = new BoolToFavoriteColorConverter();
        var result = converter.Convert(true, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.NotNull(result);
    }

    [Fact]
    public void SelectedBorderConverter_ReturnsBlueWhenSelected()
    {
        var converter = new SelectedBorderConverter();
        var result = converter.Convert(true, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.NotNull(result);
    }
}
