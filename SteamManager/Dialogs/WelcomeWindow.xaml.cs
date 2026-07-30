using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SteamManager.Dialogs;

public partial class WelcomeWindow : Wpf.Ui.Controls.FluentWindow
{
    public WelcomeWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadWhatsNewFromChangelog();

        var ease = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };

        var cards = new[] { CardWhatsNew, CardHowItWorks, CardSupport };
        var cardSlides = new System.Windows.Media.TranslateTransform[3];
        for (int i = 0; i < 3; i++)
        {
            cardSlides[i] = new System.Windows.Media.TranslateTransform(0, 20);
            cards[i].RenderTransform = cardSlides[i];
            cards[i].Opacity = 0;
        }

        var footerSlide = new System.Windows.Media.TranslateTransform(0, 15);
        FooterBorder.RenderTransform = footerSlide;
        FooterBorder.Opacity = 0;

        await Task.Delay(100);

        for (int i = 0; i < 3; i++)
        {
            cards[i].BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)) { EasingFunction = ease });
            cardSlides[i].BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new System.Windows.Media.Animation.DoubleAnimation(20, 0, TimeSpan.FromSeconds(0.3)) { EasingFunction = ease });
            await Task.Delay(100);
        }

        FooterBorder.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.25)) { EasingFunction = ease });
        footerSlide.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new System.Windows.Media.Animation.DoubleAnimation(15, 0, TimeSpan.FromSeconds(0.25)) { EasingFunction = ease });
    }

    private void LoadWhatsNewFromChangelog()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("SteamManager.Resources.WhatsNew.txt");
            if (stream == null) { SetFallbackWhatsNew(); return; }

            using var reader = new StreamReader(stream);
            var lines = reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0) { SetFallbackWhatsNew(); return; }

            WhatsNewHeader.Text = $"What's new in v{Config.AssemblyVersion}";
            foreach (var line in lines)
            {
                WhatsNewItems.Children.Add(new TextBlock
                {
                    FontSize = 11,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8A, 0x8A, 0x8A)),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 18,
                    Text = $"• {line.Trim()}",
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }
        }
        catch
        {
            SetFallbackWhatsNew();
        }
    }

    private void SetFallbackWhatsNew()
    {
        WhatsNewHeader.Text = $"What's new in v{Config.AssemblyVersion}";
        WhatsNewItems.Children.Add(new TextBlock
        {
            FontSize = 11,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8A, 0x8A, 0x8A)),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 18,
            Text = "• Achievement management features",
            Margin = new Thickness(0, 2, 0, 0)
        });
    }

    public static bool ShouldShow()
    {
        try
        {
            var flagPath = Path.Combine(Config.AppDataPath, Config.WelcomeSentinelFile);
            if (!File.Exists(flagPath)) return true;
            var content = File.ReadAllText(flagPath);
            return content != Config.AssemblyVersion;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WelcomeWindow.ShouldShow failed: {ex.Message}");
            return true;
        }
    }

    private void Kofi_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Config.KofiUrl,
                UseShellExecute = true,
            });
        }
        catch (Exception ex) { Debug.WriteLine($"Failed to open Ko-fi: {ex.Message}"); }
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
        catch (Exception ex) { Debug.WriteLine($"Failed to open GitHub sponsor: {ex.Message}"); }
    }

    private void GetStarted_Click(object sender, RoutedEventArgs e)
    {
        if (DontShowAgainCheck.IsChecked == true)
        {
            try
            {
                Directory.CreateDirectory(Config.AppDataPath);
                var flagPath = Path.Combine(Config.AppDataPath, Config.WelcomeSentinelFile);
                File.WriteAllText(flagPath, Config.AssemblyVersion);
            }
            catch (Exception ex) { Debug.WriteLine($"Failed to write welcome sentinel: {ex.Message}"); }
        }

        Close();
    }
}
