using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamManager.Converters;

public class PercentToArcMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return null!;

        if (values[0] is not int unlocked || values[1] is not int total || total == 0)
            return null!;

        double percentage = (double)unlocked / total * 100;
        percentage = Math.Min(percentage, 100);

        double centerX = 24;
        double centerY = 24;
        double radius = 20;
        double startAngleRad = -90 * Math.PI / 180;

        double sweepAngleRad = (percentage / 100.0) * 2 * Math.PI;

        if (percentage >= 99.5)
        {
            sweepAngleRad = 2 * Math.PI - 0.01;
        }

        double endX = centerX + radius * Math.Cos(startAngleRad + sweepAngleRad);
        double endY = centerY + radius * Math.Sin(startAngleRad + sweepAngleRad);

        bool isLargeArc = percentage > 50;

        if (percentage <= 0)
        {
            return null!;
        }

        string pathData;
        if (percentage >= 99.5)
        {
            pathData = string.Format(CultureInfo.InvariantCulture,
                "M {0} {1} A {2} {2} 0 1 1 {3} {1}",
                centerX, centerY - radius, radius, centerX - 0.001);
        }
        else
        {
            pathData = string.Format(CultureInfo.InvariantCulture,
                "M {0} {1} A {2} {2} 0 {3} 1 {4} {5}",
                centerX, centerY - radius, radius, isLargeArc ? 1 : 0, endX, endY);
        }

        return Geometry.Parse(pathData);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PercentToTextMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return "0.0% complete";

        if (values[0] is not int unlocked || values[1] is not int total || total == 0)
            return "0.0% complete";

        double percentage = (double)unlocked / total * 100;
        return string.Format(CultureInfo.InvariantCulture, "{0:F1}% complete", percentage);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
