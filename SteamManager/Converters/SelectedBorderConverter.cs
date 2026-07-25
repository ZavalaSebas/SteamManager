using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamManager.Converters;

public class SelectedBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));
        }
        return new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
