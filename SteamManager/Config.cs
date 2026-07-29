using System.IO;

namespace SteamManager;

public static class Config
{
    public const string AppName = "SteamManager";
    public const string SteamDll = "steamclient.dll";
    public const string SteamRegistryKey = @"HKEY_LOCAL_MACHINE\Software\Valve\Steam";
    public const string SteamInstallPathValue = "InstallPath";
    public const uint SpacewarAppId = 480;
    public const int CallbackTimerMs = 100;
    public const int RequestTimeoutSeconds = 10;
    public const string GitHubApiUrl = "https://api.github.com/repos/ZavalaSebas/SteamManager/releases/latest";
    public const string SteamCommunityUrl = "https://steamcommunity.com";
    public static string UserAgent => $"SteamManager/{AssemblyVersion}";

    public static string AppDataPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

    public const string WelcomeSentinelFile = "welcome.flag";

    public const string KofiUrl = "https://ko-fi.com/sebastianzavala82573";
    public const string GitHubSponsorUrl = "https://github.com/sponsors/ZavalaSebas?frequency=one-time";
    public const string RepoUrl = "https://github.com/ZavalaSebas/SteamManager";

    public static string CachePath =>
        Path.Combine(AppDataPath, "cache");

    public static string ImageCachePath =>
        Path.Combine(CachePath, "images");

    public static string ConfigFilePath =>
        Path.Combine(AppDataPath, "config.json");

    public static string AssemblyVersion =>
        typeof(Config).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
}
