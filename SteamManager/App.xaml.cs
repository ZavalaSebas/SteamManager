using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SteamManager.Steam;

namespace SteamManager;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static SteamContext? SteamContext { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        InitializeSteam();
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
    }

    private static void InitializeSteam()
    {
        SteamContext = Services.GetRequiredService<SteamContext>();

        uint appId = Config.SpacewarAppId;
        if (SteamContext.Initialize(appId))
        {
            SteamContext.RequestStats();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SteamContext?.Dispose();
        base.OnExit(e);
    }
}
