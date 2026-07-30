using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SteamManager.Controls;

public partial class GameCard : UserControl
{
    public static readonly RoutedEvent GameSelectedEvent = EventManager.RegisterRoutedEvent(
        "GameSelected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GameCard));

    public static readonly RoutedEvent FavoriteToggleEvent = EventManager.RegisterRoutedEvent(
        "FavoriteToggle", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GameCard));

    public event RoutedEventHandler GameSelected
    {
        add => AddHandler(GameSelectedEvent, value);
        remove => RemoveHandler(GameSelectedEvent, value);
    }

    public event RoutedEventHandler FavoriteToggle
    {
        add => AddHandler(FavoriteToggleEvent, value);
        remove => RemoveHandler(FavoriteToggleEvent, value);
    }

    public GameCard()
    {
        InitializeComponent();
        MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement element)
        {
            if (element.Name == "FavoriteButton" || element.Parent is Button)
                return;
        }
        RaiseEvent(new RoutedEventArgs(GameSelectedEvent, this));
    }

    private void OnFavoriteClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;

        var sb = TryFindResource("FavoritePop") as Storyboard;
        sb?.Begin(this);

        RaiseEvent(new RoutedEventArgs(FavoriteToggleEvent, this));
    }
}
