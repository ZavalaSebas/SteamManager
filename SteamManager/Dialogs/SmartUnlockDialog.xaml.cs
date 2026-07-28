using System.Windows;

namespace SteamManager.Dialogs;

public partial class SmartUnlockDialog : Window
{
    public int MinDelay { get; private set; } = 15;
    public int MaxDelay { get; private set; } = 45;
    public bool ShowOverlay { get; private set; } = true;

    public SmartUnlockDialog()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
    }

    public SmartUnlockDialog(int currentMinDelay, int currentMaxDelay, bool showOverlay) : this()
    {
        MinDelay = currentMinDelay;
        MaxDelay = currentMaxDelay;
        ShowOverlay = showOverlay;

        MinDelayTextBox.Text = currentMinDelay.ToString();
        MaxDelayTextBox.Text = currentMaxDelay.ToString();
        ShowOverlayCheckBox.IsChecked = showOverlay;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MinDelayTextBox.Text, out int minDelay) || minDelay < 0)
        {
            MessageBox.Show("Please enter a valid minimum delay (non-negative integer).",
                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(MaxDelayTextBox.Text, out int maxDelay) || maxDelay < 0)
        {
            MessageBox.Show("Please enter a valid maximum delay (non-negative integer).",
                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (minDelay > maxDelay)
        {
            MessageBox.Show("Minimum delay cannot be greater than maximum delay.",
                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MinDelay = minDelay;
        MaxDelay = maxDelay;
        ShowOverlay = ShowOverlayCheckBox.IsChecked ?? true;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
