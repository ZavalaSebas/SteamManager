using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SteamManager.Models;

namespace SteamManager.Controls;

public partial class AchievementCard : UserControl
{
    public static readonly DependencyProperty ToggleCommandProperty =
        DependencyProperty.Register(nameof(ToggleCommand), typeof(ICommand), typeof(AchievementCard));

    public ICommand ToggleCommand
    {
        get => (ICommand)GetValue(ToggleCommandProperty);
        set => SetValue(ToggleCommandProperty, value);
    }

    public AchievementCard()
    {
        InitializeComponent();
    }

    private void CardBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is AchievementInfo achievement && ToggleCommand?.CanExecute(achievement) == true)
        {
            ToggleCommand.Execute(achievement);
        }
    }
}

public class UnlockStatusToColorConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isUnlocked)
            return isUnlocked ? new SolidColorBrush(Color.FromRgb(0x2E, 0xA8, 0x2E)) : new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
        return new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
