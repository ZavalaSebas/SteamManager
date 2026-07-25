using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SteamManager.Steam;

/// <summary>
/// Loads steamclient.dll from the Steam installation directory.
/// Uses Windows Registry to find the Steam install path.
/// </summary>
public static class SteamLoader
{
    private static IntPtr _dllHandle;
    private static IntPtr _createInterfacePtr;
    private static IntPtr _bGetCallbackPtr;
    private static IntPtr _freeLastCallbackPtr;

    public static bool IsLoaded => _dllHandle != IntPtr.Zero;

    /// <summary>
    /// Finds the Steam installation path from the Windows Registry.
    /// </summary>
    public static string? GetSteamInstallPath()
    {
        return Registry.GetValue(Config.SteamRegistryKey, Config.SteamInstallPathValue, null) as string;
    }

    /// <summary>
    /// Loads steamclient.dll and resolves the 3 required exported functions.
    /// </summary>
    public static bool Load()
    {
        if (IsLoaded)
            return true;

        string? steamPath = GetSteamInstallPath();
        if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath))
            return false;

        string dllPath = Path.Combine(steamPath, Config.SteamDll);
        if (!File.Exists(dllPath))
            return false;

        // Add Steam directories to DLL search path so steamclient.dll dependencies resolve.
        // Matches SAM's approach (semicolon-joined path).
        NativeMethods.SetDllDirectory(steamPath + ";" + Path.Combine(steamPath, "bin"));

        // Load the DLL with altered search path so its directory is searched for deps
        _dllHandle = NativeMethods.LoadLibraryEx(
            dllPath,
            IntPtr.Zero,
            NativeMethods.LoadWithAlteredSearchPath);

        if (_dllHandle == IntPtr.Zero)
            return false;

        // Resolve exported functions
        _createInterfacePtr = NativeMethods.GetProcAddress(_dllHandle, "CreateInterface");
        _bGetCallbackPtr = NativeMethods.GetProcAddress(_dllHandle, "Steam_BGetCallback");
        _freeLastCallbackPtr = NativeMethods.GetProcAddress(_dllHandle, "Steam_FreeLastCallback");

        return _createInterfacePtr != IntPtr.Zero
            && _bGetCallbackPtr != IntPtr.Zero
            && _freeLastCallbackPtr != IntPtr.Zero;
    }

    /// <summary>
    /// Creates a Steam interface object by version string.
    /// Returns the object pointer, or IntPtr.Zero on failure.
    /// </summary>
    public static IntPtr CreateInterface(string version)
    {
        if (_createInterfacePtr == IntPtr.Zero)
            return IntPtr.Zero;

        var createInterface = Marshal.GetDelegateForFunctionPointer<CreateInterfaceDelegate>(_createInterfacePtr);
        return createInterface(version, IntPtr.Zero);
    }

    /// <summary>
    /// Polls for the next pending callback from the Steam IPC pipe.
    /// </summary>
    public static bool GetCallback(int pipe, out CallbackMessage message, out int call)
    {
        message = default;
        call = 0;

        if (_bGetCallbackPtr == IntPtr.Zero)
            return false;

        var getCallback = Marshal.GetDelegateForFunctionPointer<GetCallbackDelegate>(_bGetCallbackPtr);
        return getCallback(pipe, out message, out call);
    }

    /// <summary>
    /// Frees the last retrieved callback.
    /// </summary>
    public static bool FreeLastCallback(int pipe)
    {
        if (_freeLastCallbackPtr == IntPtr.Zero)
            return false;

        var freeCallback = Marshal.GetDelegateForFunctionPointer<FreeLastCallbackDelegate>(_freeLastCallbackPtr);
        return freeCallback(pipe);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private delegate IntPtr CreateInterfaceDelegate(string version, IntPtr returnCode);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool GetCallbackDelegate(int pipe, out CallbackMessage message, out int call);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool FreeLastCallbackDelegate(int pipe);
}