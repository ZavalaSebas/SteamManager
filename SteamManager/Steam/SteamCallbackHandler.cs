using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// Handles dispatching Steam API callbacks to registered handlers.
/// Uses Steam_BGetCallback polling approach.
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
    /// Dispatches pending callbacks using Steam_BGetCallback.
    /// Called after SteamAPI_RunCallbacks() in the original SAM.
    /// </summary>
    public void DispatchPending()
    {
        CallbackMessage message;
        int call;

        while (SteamLoader.GetCallback(0, out message, out call))
        {
            int callbackId = message.Id;

            if (_handlers.TryGetValue(callbackId, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler(message.ParamPointer);
                }
            }

            SteamLoader.FreeLastCallback(0);
        }
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
