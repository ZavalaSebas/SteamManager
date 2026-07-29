using System.Diagnostics;
using Wpf.Ui.Controls;
using Microsoft.Extensions.DependencyInjection;
using SteamManager.ViewModels;

namespace SteamManager;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        if (DataContext == null)
        {
            DataContext = App.Services.GetRequiredService<MainViewModel>();
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            if (base.WindowState == System.Windows.WindowState.Maximized)
                base.WindowState = System.Windows.WindowState.Normal;
            else
                base.WindowState = System.Windows.WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void Heart_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Config.KofiUrl,
                UseShellExecute = true,
            });
        }
        catch { }
    }
}
