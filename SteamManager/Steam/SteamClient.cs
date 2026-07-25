namespace SteamManager.Steam;

/// <summary>
/// Manages the Steam API lifecycle using steamclient.dll.
/// Handles initialization, pipe/user creation, and callback dispatch.
/// </summary>
public class SteamClient : IDisposable
{
    private readonly SteamCallbackHandler _callbackHandler;
    private SteamClient018? _steamClient;
    private SteamUserStats013? _userStats;
    private SteamApps008? _steamApps;
    private SteamUtils005? _steamUtils;
    private int _pipe;
    private int _user;
    private bool _initialized;

    public bool IsInitialized => _initialized;
    public uint CurrentAppId { get; private set; }
    public SteamCallbackHandler CallbackHandler => _callbackHandler;
    public SteamUserStats013 UserStats => _userStats ?? throw new InvalidOperationException("Steam not initialized");
    public SteamApps008 Apps => _steamApps ?? throw new InvalidOperationException("Steam not initialized");
    public SteamUtils005 Utils => _steamUtils ?? throw new InvalidOperationException("Steam not initialized");

    public SteamClient()
    {
        _callbackHandler = new SteamCallbackHandler();
    }

    /// <summary>
    /// Initializes the Steam API for the specified app.
    /// Must be called before any other Steam API calls.
    /// </summary>
    public bool Init(uint appId)
    {
        if (_initialized)
            return true;

        CurrentAppId = appId;
        Environment.SetEnvironmentVariable("SteamAppId", appId.ToString());

        // Load steamclient.dll
        if (!SteamLoader.Load())
            return false;

        // Create root client object
        IntPtr clientObj = SteamLoader.CreateInterface("SteamClient018");
        if (clientObj == IntPtr.Zero)
            return false;

        _steamClient = new SteamClient018();
        _steamClient.SetupFunctions(clientObj);

        // Create IPC pipe
        _pipe = _steamClient.CreateSteamPipe();
        if (_pipe == 0)
            return false;

        // Connect to global user
        _user = _steamClient.ConnectToGlobalUser(_pipe);
        if (_user == 0)
            return false;

        // Get ISteamUtils first to verify appID (matches SAM initialization order)
        IntPtr utilsObj = _steamClient.GetISteamUtils(_pipe, "SteamUtils005");
        if (utilsObj == IntPtr.Zero)
            return false;

        _steamUtils = new SteamUtils005();
        _steamUtils.SetupFunctions(utilsObj);

        // Verify our runtime AppId matches what Steam sees
        if (appId > 0)
        {
            uint steamAppId = _steamUtils.GetAppId();
            if (steamAppId != appId)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"AppID mismatch: expected {appId}, got {steamAppId}");
                return false;
            }
        }

        // Get ISteamUserStats interface
        IntPtr userStatsObj = _steamClient.GetISteamUserStats(
            _user, _pipe, "STEAMUSERSTATS_INTERFACE_VERSION013");
        if (userStatsObj == IntPtr.Zero)
            return false;

        _userStats = new SteamUserStats013();
        _userStats.SetupFunctions(userStatsObj);

        // Get ISteamApps interface
        IntPtr appsObj = _steamClient.GetISteamApps(
            _user, _pipe, "STEAMAPPS_INTERFACE_VERSION008");
        if (appsObj == IntPtr.Zero)
            return false;

        _steamApps = new SteamApps008();
        _steamApps.SetupFunctions(appsObj);

        _initialized = true;
        return true;
    }

    /// <summary>
    /// Requests current stats from Steam servers.
    /// After calling this, wait for the UserStatsReceived_t callback.
    /// </summary>
    public bool RequestCurrentStats()
    {
        EnsureInitialized();
        // Stats are automatically requested when the interface is created.
        return true;
    }

    /// <summary>
    /// Runs all pending Steam API callbacks.
    /// Should be called periodically (e.g., on a timer).
    /// </summary>
    public void RunCallbacks()
    {
        if (!_initialized)
            return;

        _callbackHandler.DispatchPending();
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("Steam API not initialized. Call Init() first.");
    }

    public void Dispose()
    {
        if (_initialized)
        {
            _steamClient?.ReleaseUser(_pipe, _user);
            _steamClient?.ReleaseSteamPipe(_pipe);
            _initialized = false;
        }
        GC.SuppressFinalize(this);
    }
}