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
    private SteamUser012? _steamUser;
    private SteamApps001? _steamApps001;
    private int _pipe;
    private int _user;
    private bool _initialized;

    public bool IsInitialized => _initialized;
    public uint CurrentAppId { get; private set; }
    public SteamCallbackHandler CallbackHandler => _callbackHandler;
    public SteamUserStats013 UserStats => _userStats ?? throw new InvalidOperationException("Steam not initialized");
    public SteamApps008 Apps => _steamApps ?? throw new InvalidOperationException("Steam not initialized");
    public SteamUtils005 Utils => _steamUtils ?? throw new InvalidOperationException("Steam not initialized");
    public SteamUser012 User => _steamUser ?? throw new InvalidOperationException("Steam not initialized");
    public SteamApps001 Apps001 => _steamApps001 ?? throw new InvalidOperationException("Steam not initialized");

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

        string logFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "steammanager_init.txt");
        void Log(string msg) => System.IO.File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");

        Log($"Init({appId}) starting");

        CurrentAppId = appId;
        Environment.SetEnvironmentVariable("SteamAppId", appId.ToString());
        Log("Set SteamAppId env var");

        // Load steamclient.dll
        if (!SteamLoader.Load())
        {
            Log("SteamLoader.Load() failed");
            return false;
        }
        Log("Steam loaded");

        // Create root client object
        IntPtr clientObj = SteamLoader.CreateInterface("SteamClient018");
        if (clientObj == IntPtr.Zero)
        {
            Log("CreateInterface returned null");
            return false;
        }
        Log($"CreateInterface got clientObj=0x{clientObj:X}");

        _steamClient = new SteamClient018();
        _steamClient.SetupFunctions(clientObj);

        // Create IPC pipe
        _pipe = _steamClient.CreateSteamPipe();
        Log($"CreateSteamPipe returned pipe={_pipe}");
        if (_pipe == 0)
            return false;

        // Connect to global user
        _user = _steamClient.ConnectToGlobalUser(_pipe);
        Log($"ConnectToGlobalUser returned user={_user}");
        if (_user == 0)
            return false;

        // Get ISteamUtils first to verify appID (matches SAM initialization order)
        IntPtr utilsObj = _steamClient.GetISteamUtils(_pipe, "SteamUtils005");
        if (utilsObj == IntPtr.Zero)
        {
            Log("GetISteamUtils returned null");
            return false;
        }
        Log($"GetISteamUtils got utilsObj=0x{utilsObj:X}");

        _steamUtils = new SteamUtils005();
        _steamUtils.SetupFunctions(utilsObj);

        uint actualAppId = _steamUtils.GetAppId();
        Log($"GetAppId returned {actualAppId}");

        // Get ISteamUserStats interface
        IntPtr userStatsObj = _steamClient.GetISteamUserStats(
            _user, _pipe, "STEAMUSERSTATS_INTERFACE_VERSION013");
        if (userStatsObj == IntPtr.Zero)
        {
            Log("GetISteamUserStats returned null");
            return false;
        }
        Log("Got UserStats interface");

        _userStats = new SteamUserStats013();
        _userStats.SetupFunctions(userStatsObj);

        // Get ISteamApps interface
        IntPtr appsObj = _steamClient.GetISteamApps(
            _user, _pipe, "STEAMAPPS_INTERFACE_VERSION008");
        if (appsObj == IntPtr.Zero)
        {
            Log("GetISteamApps returned null");
            return false;
        }
        Log("Got Apps interface");

        _steamApps = new SteamApps008();
        _steamApps.SetupFunctions(appsObj);

        // Get ISteamUser interface (for SteamID)
        IntPtr userObj = _steamClient.GetISteamUser(_user, _pipe, "SteamUser012");
        if (userObj == IntPtr.Zero)
        {
            Log("GetISteamUser returned null");
            return false;
        }
        Log("Got User interface");

        _steamUser = new SteamUser012();
        _steamUser.SetupFunctions(userObj);

        // Get ISteamApps001 interface (for GetAppData)
        IntPtr apps001Obj = _steamClient.GetISteamApps(
            _user, _pipe, "STEAMAPPS_INTERFACE_VERSION001");
        if (apps001Obj == IntPtr.Zero)
        {
            Log("GetISteamApps(001) returned null");
            return false;
        }
        Log("Got Apps001 interface");

        _steamApps001 = new SteamApps001();
        _steamApps001.SetupFunctions(apps001Obj);

        _initialized = true;
        Log($"Init({appId}) succeeded! actualAppId={actualAppId}");
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