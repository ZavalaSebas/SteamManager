using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamManager.Converters;

public class ProgressToArcConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percentage && percentage >= 0)
        {
            percentage = Math.Min(percentage, 100);

            double centerX = 11;
            double centerY = 11;
            double radius = 8.5;
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
                pathData = $"M {centerX} {centerY - radius} " +
                           $"A {radius} {radius} 0 1 1 {centerX - 0.001} {centerY - radius}";
            }
            else
            {
                pathData = $"M {centerX} {centerY - radius} " +
                           $"A {radius} {radius} 0 {(isLargeArc ? 1 : 0)} 1 {endX:F2} {endY:F2}";
            }

            return Geometry.Parse(pathData);
        }
        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
