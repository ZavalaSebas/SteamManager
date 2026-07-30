using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace SteamManager.Dialogs;

public partial class AboutDialog : Wpf.Ui.Controls.FluentWindow
{
    public AboutDialog()
    {
        InitializeComponent();
        VersionText.Text = $"Version {Config.AssemblyVersion}";
        Owner = Application.Current.MainWindow;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Support_Click(object sender, RoutedEventArgs e)
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

    private void GitHubSponsor_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Config.GitHubSponsorUrl,
                UseShellExecute = true,
            });
        }
        catch { }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
