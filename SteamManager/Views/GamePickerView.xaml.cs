using System.Windows.Controls;

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
}
