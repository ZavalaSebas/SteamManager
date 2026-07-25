using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SteamManager.Models;

namespace SteamManager.Views;

public partial class GamePickerView : UserControl
{
    public GamePickerView()
    {
        InitializeComponent();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is ViewModels.GamePickerViewModel vm)
        {
            vm.SearchText = ((TextBox)sender).Text;
        }
    }

    private void GameCard_GameSelected(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is GameInfo game)
        {
            if (DataContext is ViewModels.GamePickerViewModel vm)
            {
                vm.SelectGameCommand.Execute(game);
            }
        }
    }
}
