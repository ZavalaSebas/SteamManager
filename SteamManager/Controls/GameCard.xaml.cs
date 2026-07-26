using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using SteamManager.Models;

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
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GameInfo game)
        {
            UpdateFavoriteState(game.IsFavorite);
        }
    }

    private void UpdateFavoriteState(bool isFavorite)
    {
        if (FavoriteGlow != null)
        {
            FavoriteGlow.Opacity = isFavorite ? 0.6 : 0;
        }
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
        RaiseEvent(new RoutedEventArgs(FavoriteToggleEvent, this));
    }
}
