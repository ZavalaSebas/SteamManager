using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// Vtable layout for ISteamApps001 interface (matches SAM/gibbed).
/// Provides access to app metadata via GetAppData.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ISteamApps001
{
    public IntPtr GetAppData; // 0
}

/// <summary>
/// Wrapper for ISteamApps001 interface.
/// Provides access to app metadata (name, logo, etc.).
/// </summary>
public class SteamApps001 : NativeWrapper<ISteamApps001>
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int NativeGetAppData(
        IntPtr self,
        uint appId,
        IntPtr key,
        IntPtr value,
        int valueLength);

    public string? GetAppData(uint appId, string key)
    {
        using var keyHandle = NativeStrings.StringToStringHandle(key);
        const int valueLength = 1024;
        IntPtr valuePtr = Marshal.AllocHGlobal(valueLength);
        try
        {
            int result = Call<int, NativeGetAppData>(
                Functions.GetAppData,
                appId,
                keyHandle.DangerousGetHandle(),
                valuePtr,
                valueLength);

            if (result == 0)
                return null;

            return NativeStrings.PointerToString(valuePtr);
        }
        finally
        {
            Marshal.FreeHGlobal(valuePtr);
        }
    }
}
