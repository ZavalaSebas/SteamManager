using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace SteamManager.Dialogs;

public partial class SmartUnlockResultDialog : Window
{
    private DispatcherTimer? _autoCloseTimer;
    private readonly int _applied;
    private readonly int _protected;
    private readonly int _failed;
    private readonly bool _wasCancelled;

    public SmartUnlockResultDialog(int applied, int protectedCount, int failed, bool wasCancelled = false)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;

        _applied = applied;
        _protected = protectedCount;
        _failed = failed;
        _wasCancelled = wasCancelled;

        Closing += OnDialogClosing;

        bool hasProblems = protectedCount > 0 || failed > 0;

        AppliedResultText.Text = applied.ToString();
        ProtectedResultText.Text = protectedCount.ToString();
        FailedResultText.Text = failed.ToString();

        if (wasCancelled)
        {
            HeaderText.Text = "Smart Unlock Cancelled";
            SubheaderText.Text = "Operation stopped by user";
            SuccessIcon.Visibility = Visibility.Collapsed;
            WarningIcon.Visibility = Visibility.Collapsed;
            WarningPanel.Visibility = Visibility.Collapsed;
            IconBorder.Style = null;
            IconBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A));
        }
        else if (hasProblems)
        {
            HeaderText.Text = "Smart Unlock Complete";
            SubheaderText.Text = "Some achievements could not be unlocked";
            SuccessIcon.Visibility = Visibility.Collapsed;
            WarningIcon.Visibility = Visibility.Visible;
            WarningPanel.Visibility = Visibility.Visible;

            if (protectedCount > 0 && failed > 0)
            {
                WarningText.Text = $"{protectedCount} protected and {failed} failed.";
            }
            else if (protectedCount > 0)
            {
                WarningText.Text = $"{protectedCount} protected achievement(s) were skipped.";
            }
            else
            {
                WarningText.Text = $"{failed} achievement(s) failed to unlock.";
            }
        }
        else
        {
            HeaderText.Text = applied > 0 ? "Smart Unlock Complete" : "Smart Unlock";
            SubheaderText.Text = applied > 0
                ? "All achievements unlocked successfully"
                : "No achievements to unlock";
            SuccessIcon.Visibility = applied > 0 ? Visibility.Visible : Visibility.Collapsed;
            WarningIcon.Visibility = Visibility.Collapsed;
            WarningPanel.Visibility = Visibility.Collapsed;
        }

        CloseButton.Click += (s, e) =>
        {
            _autoCloseTimer?.Stop();
            DialogResult = true;
            Close();
        };

        if (!hasProblems && applied > 0 && !wasCancelled)
        {
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _autoCloseTimer.Tick += (s, e) =>
            {
                if (IsLoaded)
                {
                    _autoCloseTimer?.Stop();
                    DialogResult = true;
                    Close();
                }
            };
            _autoCloseTimer.Start();
        }
    }

    private void OnDialogClosing(object? sender, CancelEventArgs e)
    {
        _autoCloseTimer?.Stop();
    }

    public bool HasProblems => _protected > 0 || _failed > 0;
    public bool WasCancelled => _wasCancelled;
}
