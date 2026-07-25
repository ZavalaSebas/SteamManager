using System.IO;

namespace SteamManager;

/// <summary>
/// Centralized constants for the application.
/// All URLs, paths, timeouts, and magic values go here.
/// </summary>
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

    public static string CachePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName, "cache");

    public static string ImageCachePath =>
        Path.Combine(CachePath, "images");

    public static string ConfigFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName, "config.json");

    public static string AssemblyVersion =>
        typeof(Config).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
}
