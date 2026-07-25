using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SteamManager.Converters;
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

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length >= 2 && e.Args[0] == "--game")
        {
            if (uint.TryParse(e.Args[1], out uint appId))
            {
                StartGameHelperMode(appId);
                return;
            }
        }

        StartLauncherMode();
    }

    private void StartLauncherMode()
    {
        var services = new ServiceCollection();
        ConfigureLauncherServices(services);
        Services = services.BuildServiceProvider();

        var imageCacheService = Services.GetRequiredService<IImageCacheService>();
        UrlToCachedImageConverter.SetCacheService(imageCacheService);

        var mainViewModel = Services.GetRequiredService<MainViewModel>();
        var mainWindow = new MainWindow { DataContext = mainViewModel };
        mainWindow.Show();

        _ = InitializeSteamAsync(mainViewModel);
        _ = mainViewModel.LoadGamesCommand.ExecuteAsync(null);
    }

    private void StartGameHelperMode(uint appId)
    {
        var services = new ServiceCollection();
        ConfigureGameHelperServices(services, appId);
        Services = services.BuildServiceProvider();

        var imageCacheService = Services.GetRequiredService<IImageCacheService>();
        UrlToCachedImageConverter.SetCacheService(imageCacheService);

        var gameManagerVm = Services.GetRequiredService<GameManagerViewModel>();

        var game = new GameInfo { AppId = appId };
        gameManagerVm.SelectGameCommand.Execute(game);

        var mainWindow = new MainWindow { DataContext = gameManagerVm };
        mainWindow.Show();

        InitializeGameHelperSteam(gameManagerVm, appId);
    }

    private static void ConfigureLauncherServices(IServiceCollection services)
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

    private static void ConfigureGameHelperServices(IServiceCollection services, uint appId)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<SteamClient>();
        services.AddSingleton<SteamContext>();
        services.AddSingleton<IImageCacheService, ImageCacheService>();
        services.AddSingleton<ISmartUnlockService, SmartUnlockService>();
        services.AddSingleton<IConfigService, ConfigService>();

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
                    return SteamContext.Initialize(Config.SpacewarAppId);
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

    private static void InitializeGameHelperSteam(GameManagerViewModel gameManagerVm, uint appId)
    {
        SteamContext = Services.GetRequiredService<SteamContext>();

        bool initialized = Task.Run(() => SteamContext.Initialize(appId)).Result;

        if (initialized)
        {
            gameManagerVm.StatusMessage = $"Loaded {gameManagerVm.SelectedGame?.Name ?? appId.ToString()} achievements";
            gameManagerVm.LoadAchievementsCommand.Execute(null);
            StartCallbackTimer();
        }
        else
        {
            gameManagerVm.StatusMessage = "Failed to connect to Steam.";
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
