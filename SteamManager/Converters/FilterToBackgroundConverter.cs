using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SteamManager.ViewModels;

namespace SteamManager.Converters;

public class FilterToBackgroundConverter : IValueConverter
{
    private static readonly Brush BrushActive = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));
    private static readonly Brush BrushInactive = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AchievementFilterType currentFilter && parameter is string filterName)
        {
            if (Enum.TryParse<AchievementFilterType>(filterName, out var filter) && currentFilter == filter)
                return BrushActive;
        }
        return BrushInactive;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class FilterToForegroundConverter : IValueConverter
{
    private static readonly Brush BrushActive = new SolidColorBrush(Colors.White);
    private static readonly Brush BrushInactive = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AchievementFilterType currentFilter && parameter is string filterName)
        {
            if (Enum.TryParse<AchievementFilterType>(filterName, out var filter) && currentFilter == filter)
                return BrushActive;
        }
        return BrushInactive;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
