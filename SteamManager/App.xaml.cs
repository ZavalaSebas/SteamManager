using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SteamManager.Converters;
using SteamManager.Dialogs;
using SteamManager.Models;
using SteamManager.Services;
using SteamManager.Steam;
using SteamManager.ViewModels;

namespace SteamManager;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static SteamContext? SteamContext { get; private set; }
    private static DispatcherTimer? _callbackTimer;
    private static Mutex? _launcherMutex;
    private const string LauncherMutexName = "SteamManager_Launcher_Mutex";

    public static string? PendingUpdateTag { get; private set; }
    public static string? PendingUpdateUrl { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length >= 2 && e.Args[0] == "--game" && uint.TryParse(e.Args[1], out uint appId))
        {
            StartGameHelperMode(appId);
        }
        else
        {
            _ = SafeStartLauncherMode();
        }
    }

    private async Task SafeStartLauncherMode()
    {
        try
        {
            await StartLauncherMode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Startup failed: {ex}");
            Current.Shutdown();
        }
    }

    private async Task StartLauncherMode()
    {
        _launcherMutex = new Mutex(true, LauncherMutexName, out bool createdNew);
        if (!createdNew)
        {
            var existingWindow = Current.MainWindow;
            if (existingWindow != null)
            {
                existingWindow.WindowState = WindowState.Normal;
                existingWindow.Show();
                existingWindow.Activate();
            }
            Shutdown();
            return;
        }

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var imageCacheService = Services.GetRequiredService<IImageCacheService>();
        UrlToCachedImageConverter.SetCacheService(imageCacheService);

        var mainViewModel = Services.GetRequiredService<MainViewModel>();
        var mainWindow = new MainWindow { DataContext = mainViewModel };
        mainWindow.Closing += MainWindow_Closing;
        MainWindow = mainWindow;
        mainWindow.Show();

        Updater.CleanupOldExe();
        _ = SafeCheckForUpdateAsync(mainWindow);

        var steamInitTask = InitializeSteamAsync(mainViewModel);

        if (WelcomeWindow.ShouldShow())
        {
            var welcomeWindow = new WelcomeWindow { Owner = mainWindow };
            welcomeWindow.ShowDialog();
        }

        await steamInitTask;
        await mainViewModel.LoadGamesCommand.ExecuteAsync(null);
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _launcherMutex?.ReleaseMutex();
        _launcherMutex?.Dispose();
    }

    public static void RestoreLauncher()
    {
        if (Current.MainWindow != null)
        {
            Current.MainWindow.WindowState = WindowState.Normal;
            Current.MainWindow.Show();
            Current.MainWindow.Activate();
        }
    }

    private void StartGameHelperMode(uint appId)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var imageCacheService = Services.GetRequiredService<IImageCacheService>();
        UrlToCachedImageConverter.SetCacheService(imageCacheService);

        SteamContext = Services.GetRequiredService<SteamContext>();

        CoInitializeEx(0, COINIT_APARTMENTTHREADED);
        try
        {
            bool initialized = SteamContext.Initialize(appId);
            if (!initialized)
            {
                MessageBox.Show("Failed to initialize Steam for this game.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Steam error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var gameManagerVm = new GameManagerViewModel(SteamContext, imageCacheService);
        var gameName = SteamContext.Apps.GetAppData(appId, "name") ?? $"Game {appId}";
        var game = new GameInfo
        {
            AppId = appId,
            Name = gameName,
            CoverUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg"
        };
        gameManagerVm.SelectGameCommand.Execute(game);

        var mainVm = new MainViewModel(
            SteamContext,
            Services.GetRequiredService<IGameLibraryService>(),
            imageCacheService,
            Services.GetRequiredService<IConfigService>())
        {
            CurrentViewModel = gameManagerVm,
            StatusMessage = $"Loading {game.Name}..."
        };

        var mainWindow = new MainWindow { DataContext = mainVm };
        mainWindow.Show();

        gameManagerVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(GameManagerViewModel.StatusMessage))
            {
                mainVm.StatusMessage = gameManagerVm.StatusMessage;
            }
        };

        StartCallbackTimer();
        gameManagerVm.LoadAchievementsCommand.Execute(null);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<SteamClient>();
        services.AddSingleton<SteamContext>();
        services.AddSingleton<IGameLibraryService, SteamGameLibraryService>();
        services.AddSingleton<IImageCacheService, ImageCacheService>();
        services.AddSingleton<ISmartUnlockService>(sp => new SmartUnlockService(
            sp.GetRequiredService<SteamContext>().Achievements,
            sp.GetRequiredService<SteamContext>().Stats));
        services.AddSingleton<IConfigService, ConfigService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<GamePickerViewModel>();
        services.AddTransient<GameManagerViewModel>();
    }

    private static async Task InitializeSteamAsync(MainViewModel mainViewModel)
    {
        try
        {
            mainViewModel.StatusMessage = "Connecting to Steam...";

            SteamContext = Services.GetRequiredService<SteamContext>();

            bool initialized = await Task.Run(() =>
            {
                CoInitializeEx(0, COINIT_APARTMENTTHREADED);
                try
                {
                    return SteamContext.Initialize(Config.SpacewarAppId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Steam init failed: {ex.Message}");
                    return false;
                }
                finally
                {
                    CoUninitialize();
                }
            });

            if (initialized)
            {
                mainViewModel.StatusMessage = "Connected. Loading games...";
                StartCallbackTimer();
            }
            else
            {
                mainViewModel.StatusMessage = "Failed to connect to Steam. Running in offline mode.";
            }
        }
        catch (DllNotFoundException)
        {
            mainViewModel.StatusMessage = "steamclient.dll not found. Running in offline mode.";
        }
        catch (Exception ex)
        {
            mainViewModel.StatusMessage = $"Steam init error: {ex.Message}";
        }
    }

    private static void StartCallbackTimer()
    {
        _callbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Config.CallbackTimerMs)
        };

        _callbackTimer.Tick += (_, _) =>
        {
            try
            {
                SteamContext?.RunCallbacks();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RunCallbacks error: {ex.Message}");
            }
        };

        _callbackTimer.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _callbackTimer?.Stop();
        SteamContext?.Dispose();
        base.OnExit(e);
    }

    private const int COINIT_APARTMENTTHREADED = 0x2;

    [DllImport("ole32.dll")]
    private static extern void CoInitializeEx(IntPtr pvReserved, int dwCoInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    private static async Task SafeCheckForUpdateAsync(Window ownerWindow)
    {
        try { await CheckForUpdateAsync(ownerWindow); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Update check failed: {ex}"); }
    }

    private static async Task CheckForUpdateAsync(Window ownerWindow)
    {
        try
        {
            var (needsUpdate, tagName, downloadUrl) = await Updater.CheckForUpdateAsync();

            if (!needsUpdate || string.IsNullOrEmpty(downloadUrl)) return;

            var updateWindow = new UpdateWindow(tagName!, downloadUrl) { Owner = ownerWindow };
            updateWindow.ShowDialog();

            if (!updateWindow.WasSkipped)
            {
                return;
            }

            PendingUpdateTag = tagName;
            PendingUpdateUrl = downloadUrl;
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Update check failed: {ex}"); }
    }
}
