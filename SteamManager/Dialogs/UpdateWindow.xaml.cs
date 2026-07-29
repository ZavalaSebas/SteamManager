using System;
using System.Windows;
using SteamManager.Services;

namespace SteamManager.Dialogs;

public partial class UpdateWindow : Window
{
    private readonly string _tagName;
    private readonly string _downloadUrl;

    public bool WasSkipped { get; private set; }

    public UpdateWindow(string tagName, string downloadUrl)
    {
        InitializeComponent();
        _tagName = tagName;
        _downloadUrl = downloadUrl;
        VersionText.Text = $"SteamManager {tagName} is available";
    }

    private async void UpdateNow_Click(object sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        SkipButton.IsEnabled = false;
        ProgressPanel.Visibility = Visibility.Visible;

        var success = await Updater.DownloadAndApplyUpdateAsync(_downloadUrl,
            new Progress<double>(p =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    var pct = Math.Min(100, (int)(p * 100));
                    ProgressBar.Width = (ActualWidth - 48) * p;
                    ProgressText.Text = pct >= 100 ? "Restarting..." : $"{pct}%";
                });
            }));

        if (!success)
        {
            ProgressText.Text = "Update failed";
            UpdateButton.IsEnabled = true;
            SkipButton.IsEnabled = true;
        }
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        WasSkipped = true;
        Close();
    }
}
