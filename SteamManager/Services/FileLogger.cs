using System.IO;
using System.Text;

namespace SteamManager.Services;

public static class FileLogger
{
    private static readonly object _lock = new();
    private static string? _logPath;

    public static void Initialize(string? logDir = null)
    {
        logDir ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SteamManager",
            "logs");

        Directory.CreateDirectory(logDir);

        _logPath = Path.Combine(logDir, $"gameloader_{DateTime.Now:yyyyMMdd_HHmmss}.log");
    }

    public static void Log(string message)
    {
        if (_logPath == null) return;

        lock (_lock)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }
    }

    public static void LogSection(string title)
    {
        if (_logPath == null) return;

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logPath, Environment.NewLine + $"=== {title} ===" + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }
    }

    public static string? GetLastLogPath() => _logPath;
}
