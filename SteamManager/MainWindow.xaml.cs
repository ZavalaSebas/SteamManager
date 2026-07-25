using Wpf.Ui.Controls;
using Microsoft.Extensions.DependencyInjection;
using SteamManager.ViewModels;

namespace SteamManager;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }
}
