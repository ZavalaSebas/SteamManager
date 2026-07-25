using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SteamManager.Converters;
using SteamManager.Services;
using SteamManager.Steam;
using SteamManager.ViewModels;

namespace SteamManager;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static SteamContext? SteamContext { get; private set; }
    private static DispatcherTimer? _callbackTimer;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var imageCacheService = Services.GetRequiredService<IImageCacheService>();
        UrlToCachedImageConverter.SetCacheService(imageCacheService);

        var mainViewModel = Services.GetRequiredService<MainViewModel>();
        var mainWindow = new MainWindow { DataContext = mainViewModel };
        mainWindow.Show();

        await InitializeSteamAsync(mainViewModel);

        await mainViewModel.LoadGamesCommand.ExecuteAsync(null);
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
        services.AddSingleton<ISmartUnlockService, SmartUnlockService>();
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
                try
                {
                    uint appId = Config.SpacewarAppId;
                    return SteamContext.Initialize(appId);
                }
                catch
                {
                    return false;
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
            catch
            {
                // Silently ignore callback errors
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
}
