using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamManager.Converters;

public class GlobalPercentageToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not float percent || percent < 0)
            return new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x5A));

        return percent switch
        {
            >= 50f => new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71)),
            >= 10f => new SolidColorBrush(Color.FromRgb(0xF1, 0xC4, 0x0F)),
            _ => new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class GlobalPercentageToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not float percent || percent < 0)
            return string.Empty;

        return $"Rarity: {percent:F1}%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
