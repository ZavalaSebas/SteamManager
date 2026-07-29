using System.Diagnostics;
using System.Windows;

namespace SteamManager.Dialogs;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        VersionText.Text = $"Version {Config.AssemblyVersion}";
        Owner = Application.Current.MainWindow;
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

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
