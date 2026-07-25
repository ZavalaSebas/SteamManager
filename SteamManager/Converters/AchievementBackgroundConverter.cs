using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamManager.Converters;

public class AchievementBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool? isUnlocked = value as bool?;

        if (isUnlocked == true)
        {
            return new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(0x2A, 0x3A, 0x2A), 0),
                    new GradientStop(Color.FromRgb(0x1E, 0x2E, 0x1E), 1)
                },
                90);
        }
        else if (isUnlocked == false)
        {
            string? state = parameter as string;
            if (state == "Hidden")
            {
                return new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(0x2A, 0x20, 0x3A), 0),
                        new GradientStop(Color.FromRgb(0x1E, 0x15, 0x2E), 1)
                    },
                    90);
            }
            return new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(0x2A, 0x2A, 0x2A), 0),
                    new GradientStop(Color.FromRgb(0x1E, 0x1E, 0x1E), 1)
                },
                90);
        }

        return new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x25));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
