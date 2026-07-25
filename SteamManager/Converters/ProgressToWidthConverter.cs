using System.Globalization;
using System.Windows.Data;

namespace SteamManager.Converters;

public class ProgressToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            double maxWidth = 192;
            return progress / 100.0 * maxWidth;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
