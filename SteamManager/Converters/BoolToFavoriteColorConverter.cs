using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamManager.Converters;

public class BoolToFavoriteColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isFavorite && isFavorite)
        {
            return new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00));
        }
        return new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
