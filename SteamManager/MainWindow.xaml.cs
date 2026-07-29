using System.Diagnostics;
using System.Windows;
using Wpf.Ui.Controls;
using Microsoft.Extensions.DependencyInjection;
using SteamManager.Dialogs;
using SteamManager.Services;
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

    private void HamburgerMenu_Click(object sender, RoutedEventArgs e)
    {
        HamburgerPopup.IsOpen = !HamburgerPopup.IsOpen;
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        HamburgerPopup.IsOpen = false;
        var aboutDialog = new AboutDialog { Owner = this };
        aboutDialog.ShowDialog();
    }

    private async void CheckUpdatesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        HamburgerPopup.IsOpen = false;

        try
        {
            var (needsUpdate, tagName, downloadUrl) = await Updater.CheckForUpdateAsync();

            if (!needsUpdate || string.IsNullOrEmpty(downloadUrl))
            {
                var noUpdateDialog = new InfoDialog("No Update Available",
                    $"You're running the latest version ({Config.AssemblyVersion}).",
                    "No new updates are available at this time.");
                noUpdateDialog.Owner = this;
                noUpdateDialog.ShowDialog();
                return;
            }

            var updateWindow = new UpdateWindow(tagName!, downloadUrl) { Owner = this };
            updateWindow.ShowDialog();
        }
        catch
        {
            var errorDialog = new InfoDialog("Update Check Failed",
                "Unable to check for updates.",
                "Please try again later.");
            errorDialog.Owner = this;
            errorDialog.ShowDialog();
        }
    }

    private void SupportMenuItem_Click(object sender, RoutedEventArgs e)
    {
        HamburgerPopup.IsOpen = false;
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

    private void KofiMenuItem_Click(object sender, RoutedEventArgs e)
    {
        HamburgerPopup.IsOpen = false;
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

    private void GitHubSponsorMenuItem_Click(object sender, RoutedEventArgs e)
    {
        HamburgerPopup.IsOpen = false;
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
}
