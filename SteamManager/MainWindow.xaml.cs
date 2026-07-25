using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SteamManager.ViewModels;

namespace SteamManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }
}
