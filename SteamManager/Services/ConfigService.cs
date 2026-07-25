using System.IO;
using System.Text.Json;

namespace SteamManager.Services;

public class AppConfig
{
    public List<uint> FavoriteGameIds { get; set; } = [];
    public uint? LastSelectedGameId { get; set; }
    public int MinUnlockDelaySeconds { get; set; } = 15;
    public int MaxUnlockDelaySeconds { get; set; } = 45;
    public string Theme { get; set; } = "Dark";
}

public class ConfigService : IConfigService
{
    private readonly string _configPath;
    private AppConfig _config = new();
    private readonly object _lock = new();

    public ConfigService()
    {
        _configPath = Config.ConfigFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        Load();
    }

    public List<uint> FavoriteGameIds
    {
        get
        {
            lock (_lock)
            {
                return [.. _config.FavoriteGameIds];
            }
        }
    }

    public uint? LastSelectedGameId
    {
        get
        {
            lock (_lock)
            {
                return _config.LastSelectedGameId;
            }
        }
        set
        {
            lock (_lock)
            {
                _config.LastSelectedGameId = value;
            }
        }
    }

    public int MinUnlockDelaySeconds
    {
        get
        {
            lock (_lock)
            {
                return _config.MinUnlockDelaySeconds;
            }
        }
        set
        {
            lock (_lock)
            {
                _config.MinUnlockDelaySeconds = value;
            }
        }
    }

    public int MaxUnlockDelaySeconds
    {
        get
        {
            lock (_lock)
            {
                return _config.MaxUnlockDelaySeconds;
            }
        }
        set
        {
            lock (_lock)
            {
                _config.MaxUnlockDelaySeconds = value;
            }
        }
    }

    public string Theme
    {
        get
        {
            lock (_lock)
            {
                return _config.Theme;
            }
        }
        set
        {
            lock (_lock)
            {
                _config.Theme = value;
            }
        }
    }

    public void AddFavorite(uint appId)
    {
        lock (_lock)
        {
            if (!_config.FavoriteGameIds.Contains(appId))
            {
                _config.FavoriteGameIds.Add(appId);
            }
        }
    }

    public void RemoveFavorite(uint appId)
    {
        lock (_lock)
        {
            _config.FavoriteGameIds.Remove(appId);
        }
    }

    public bool IsFavorite(uint appId)
    {
        lock (_lock)
        {
            return _config.FavoriteGameIds.Contains(appId);
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }
    }

    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch
            {
                _config = new AppConfig();
            }
        }
    }
}
