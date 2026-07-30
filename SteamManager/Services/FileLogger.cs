using System.IO;
using System.Text;

namespace SteamManager.Services;

public static class FileLogger
{
    private static readonly object _lock = new();
    private static StreamWriter? _writer;
    private static string? _logPath;

    public static void Initialize(string? logDir = null)
    {
        logDir ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SteamManager",
            "logs");

        Directory.CreateDirectory(logDir);

        _logPath = Path.Combine(logDir, $"gameloader_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        _writer = new StreamWriter(_logPath, append: true, Encoding.UTF8) { AutoFlush = false };
    }

    public static void Log(string message)
    {
        if (_writer == null) return;

        lock (_lock)
        {
            try
            {
                _writer.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            }
            catch { }
        }
    }

    public static void LogSection(string title)
    {
        if (_writer == null) return;

        lock (_lock)
        {
            try
            {
                _writer.WriteLine();
                _writer.WriteLine($"=== {title} ===");
            }
            catch { }
        }
    }

    public static void Flush()
    {
        if (_writer == null) return;

        lock (_lock)
        {
            try { _writer.Flush(); }
            catch { }
        }
    }

    public static string? GetLastLogPath() => _logPath;
}
