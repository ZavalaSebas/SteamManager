using System.Windows;
using System.Windows.Controls;

namespace SteamManager.Dialogs;

public partial class ProgressOverlay : UserControl
{
    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double), typeof(ProgressOverlay),
            new PropertyMetadata(0.0, OnProgressChanged));

    public static readonly DependencyProperty AppliedCountProperty =
        DependencyProperty.Register(nameof(AppliedCount), typeof(int), typeof(ProgressOverlay),
            new PropertyMetadata(0, OnCountChanged));

    public static readonly DependencyProperty ProtectedCountProperty =
        DependencyProperty.Register(nameof(ProtectedCount), typeof(int), typeof(ProgressOverlay),
            new PropertyMetadata(0, OnCountChanged));

    public static readonly DependencyProperty FailedCountProperty =
        DependencyProperty.Register(nameof(FailedCount), typeof(int), typeof(ProgressOverlay),
            new PropertyMetadata(0, OnCountChanged));

    public static readonly DependencyProperty TotalCountProperty =
        DependencyProperty.Register(nameof(TotalCount), typeof(int), typeof(ProgressOverlay),
            new PropertyMetadata(0, OnCountChanged));

    public static readonly DependencyProperty StatusMessageProperty =
        DependencyProperty.Register(nameof(StatusMessage), typeof(string), typeof(ProgressOverlay),
            new PropertyMetadata(string.Empty, OnStatusMessageChanged));

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public int AppliedCount
    {
        get => (int)GetValue(AppliedCountProperty);
        set => SetValue(AppliedCountProperty, value);
    }

    public int ProtectedCount
    {
        get => (int)GetValue(ProtectedCountProperty);
        set => SetValue(ProtectedCountProperty, value);
    }

    public int FailedCount
    {
        get => (int)GetValue(FailedCountProperty);
        set => SetValue(FailedCountProperty, value);
    }

    public int TotalCount
    {
        get => (int)GetValue(TotalCountProperty);
        set => SetValue(TotalCountProperty, value);
    }

    public string StatusMessage
    {
        get => (string)GetValue(StatusMessageProperty);
        set => SetValue(StatusMessageProperty, value);
    }

    public event EventHandler? CancelRequested;

    public ProgressOverlay()
    {
        InitializeComponent();
        CancelButton.Click += (s, e) => CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProgressOverlay overlay)
        {
            double progress = (double)e.NewValue;
            overlay.ProgressBarFill.Width = Math.Max(0, (overlay.ActualWidth > 0 ? overlay.ActualWidth : 380) * progress / 100.0);
        }
    }

    private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProgressOverlay overlay)
        {
            overlay.AppliedValueText.Text = overlay.AppliedCount.ToString();
            overlay.ProtectedValueText.Text = overlay.ProtectedCount.ToString();
            overlay.FailedValueText.Text = overlay.FailedCount.ToString();
        }
    }

    private static void OnStatusMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProgressOverlay overlay)
        {
            overlay.StatusMessageText.Text = e.NewValue as string ?? string.Empty;
        }
    }
}
