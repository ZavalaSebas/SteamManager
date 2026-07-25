namespace SteamManager.Steam;

/// <summary>
/// Manages the Steam API lifecycle: initialization, shutdown, and callback dispatch.
/// </summary>
public class SteamClient : IDisposable
{
    private readonly SteamCallbackHandler _callbackHandler;
    private IntPtr _userStatsPointer;
    private IntPtr _utilsPointer;
    private IntPtr _appsPointer;
    private bool _initialized;

    public bool IsInitialized => _initialized;
    public uint CurrentAppId { get; private set; }
    public SteamCallbackHandler CallbackHandler => _callbackHandler;

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

        if (SteamNative.SteamAPI_RestartAppIfNecessary(appId))
            return false;

        if (!SteamNative.SteamAPI_Init())
            return false;

        _userStatsPointer = SteamNative.SteamAPI_ISteamUserStats_GetSteamUserStats();
        _utilsPointer = SteamNative.SteamAPI_ISteamUtils_GetSteamUtils();
        _appsPointer = SteamNative.SteamAPI_ISteamApps_GetSteamApps();

        if (_userStatsPointer == IntPtr.Zero)
            return false;

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
        return SteamNative.SteamAPI_ISteamUserStats_RequestCurrentStats(_userStatsPointer);
    }

    /// <summary>
    /// Runs all pending Steam API callbacks.
    /// Should be called periodically (e.g., on a timer).
    /// </summary>
    public void RunCallbacks()
    {
        if (!_initialized)
            return;

        SteamNative.SteamAPI_RunCallbacks();
        _callbackHandler.DispatchPending();
    }

    /// <summary>
    /// Gets the native pointer for ISteamUserStats.
    /// </summary>
    internal IntPtr GetUserStatsPointer()
    {
        EnsureInitialized();
        return _userStatsPointer;
    }

    /// <summary>
    /// Gets the native pointer for ISteamUtils.
    /// </summary>
    internal IntPtr GetUtilsPointer()
    {
        EnsureInitialized();
        return _utilsPointer;
    }

    /// <summary>
    /// Gets the native pointer for ISteamApps.
    /// </summary>
    internal IntPtr GetAppsPointer()
    {
        EnsureInitialized();
        return _appsPointer;
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
            SteamNative.SteamAPI_Shutdown();
            _initialized = false;
        }
        GC.SuppressFinalize(this);
    }
}
