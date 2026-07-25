using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SteamManager.ViewModels;

namespace SteamManager.Converters;

public class FilterToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AchievementFilterType currentFilter && parameter is string filterName)
        {
            if (Enum.TryParse<AchievementFilterType>(filterName, out var filter) && currentFilter == filter)
            {
                return new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));
            }
        }
        return new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
