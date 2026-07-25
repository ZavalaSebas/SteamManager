using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// Handles dispatching Steam API callbacks to registered handlers.
/// Uses a polling-based approach with SteamAPI_RunCallbacks().
/// </summary>
public class SteamCallbackHandler
{
    private readonly Dictionary<int, List<Action<IntPtr>>> _handlers = new();

    /// <summary>
    /// Registers a handler for a specific callback ID.
    /// </summary>
    public void Register(int callbackId, Action<IntPtr> handler)
    {
        if (!_handlers.ContainsKey(callbackId))
            _handlers[callbackId] = new List<Action<IntPtr>>();

        _handlers[callbackId].Add(handler);
    }

    /// <summary>
    /// Dispatches pending callbacks. Called after SteamAPI_RunCallbacks().
    /// Note: With steam_api64.dll, SteamAPI_RunCallbacks() already dispatches callbacks
    /// via registered callback functions. This handler provides a managed dispatch layer
    /// for callbacks that need to be processed in our code.
    /// </summary>
    public void DispatchPending()
    {
        // With steam_api64.dll, the callback dispatch is handled internally.
        // We'll use a different pattern: register callbacks via SteamAPI_RegisterCallback.
        // For now, this is a placeholder for the managed callback system.
    }

    /// <summary>
    /// Registers a callback with the Steam API using the managed callback pattern.
    /// </summary>
    public void RegisterManagedCallback<T>(int callbackId, Action<T> handler) where T : struct
    {
        Register(callbackId, (paramPtr) =>
        {
            T param = Marshal.PtrToStructure<T>(paramPtr);
            handler(param);
        });
    }
}
