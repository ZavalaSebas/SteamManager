using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// Vtable layout for ISteamUtils005 interface (matches SAM/gibbed).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ISteamUtils005
{
    public IntPtr GetSecondsSinceAppActive;       // 0
    public IntPtr GetSecondsSinceComputerActive;  // 1
    public IntPtr GetConnectedUniverse;           // 2
    public IntPtr GetServerRealTime;              // 3
    public IntPtr GetIPCountry;                   // 4
    public IntPtr GetImageSize;                   // 5
    public IntPtr GetImageRGBA;                   // 6
    public IntPtr GetCSERIPPort;                  // 7
    public IntPtr GetCurrentBatteryPower;         // 8
    public IntPtr GetAppID;                      // 9
    public IntPtr SetOverlayNotificationPosition; // 10
    public IntPtr IsAPICallCompleted;            // 11
    public IntPtr GetAPICallFailureReason;        // 12
    public IntPtr GetAPICallResult;               // 13
    public IntPtr RunFrame;                       // 14
    public IntPtr GetIPCCallCount;                // 15
    public IntPtr SetWarningMessageHook;           // 16
    public IntPtr IsOverlayEnabled;               // 17
    public IntPtr OverlayNeedsPresent;            // 18
}

/// <summary>
/// Wrapper for ISteamUtils005 interface.
/// </summary>
public class SteamUtils005 : NativeWrapper<ISteamUtils005>
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeGetImageSize(IntPtr self, int index, out int width, out int height);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeGetImageRGBA(IntPtr self, int index, byte[] buffer, int length);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate uint NativeGetAppId(IntPtr self);

    public bool GetImageSize(int image, out int width, out int height)
    {
        var del = GetFunction<NativeGetImageSize>(Functions.GetImageSize);
        return del(ObjectAddress, image, out width, out height);
    }

    public bool GetImageRGBA(int image, byte[] buffer, int bufferSize)
    {
        var del = GetFunction<NativeGetImageRGBA>(Functions.GetImageRGBA);
        return del(ObjectAddress, image, buffer, bufferSize);
    }

    public uint GetAppId()
    {
        var del = GetFunction<NativeGetAppId>(Functions.GetAppID);
        return del(ObjectAddress);
    }
}