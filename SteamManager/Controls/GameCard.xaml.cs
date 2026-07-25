using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SteamManager.Controls;

public partial class GameCard : UserControl
{
    public static readonly RoutedEvent GameSelectedEvent = EventManager.RegisterRoutedEvent(
        "GameSelected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GameCard));

    public event RoutedEventHandler GameSelected
    {
        add => AddHandler(GameSelectedEvent, value);
        remove => RemoveHandler(GameSelectedEvent, value);
    }

    public GameCard()
    {
        InitializeComponent();
        MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(GameSelectedEvent, this));
    }
}
