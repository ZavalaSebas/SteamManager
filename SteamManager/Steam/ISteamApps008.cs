using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// Vtable layout for ISteamApps008 interface (matches SAM/gibbed).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ISteamApps008
{
    public IntPtr IsSubscribed;                 // 0
    public IntPtr IsLowViolence;               // 1
    public IntPtr IsCybercafe;                  // 2
    public IntPtr IsVACBanned;                  // 3
    public IntPtr GetCurrentGameLanguage;       // 4
    public IntPtr GetAvailableGameLanguages;    // 5
    public IntPtr IsSubscribedApp;              // 6
    public IntPtr IsDlcInstalled;              // 7
    public IntPtr GetEarliestPurchaseUnixTime; // 8
    public IntPtr IsSubscribedFromFreeWeekend; // 9
    public IntPtr GetDLCCount;                 // 10
    public IntPtr GetDLCDataByIndex;           // 11
    public IntPtr InstallDLC;                   // 12
    public IntPtr UninstallDLC;                 // 13
    public IntPtr RequestAppProofOfPurchaseKey; // 14
    public IntPtr GetCurrentBetaName;          // 15
    public IntPtr MarkContentCorrupt;           // 16
    public IntPtr GetInstalledDepots;          // 17
    public IntPtr GetAppInstallDir;            // 18
    public IntPtr IsAppInstalled;               // 19
    public IntPtr GetAppOwner;                  // 20
    public IntPtr GetLaunchQueryParam;         // 21
    public IntPtr GetDlcDownloadProgress;      // 22
    public IntPtr GetAppBuildId;                // 23
    public IntPtr RequestAllProofOfPurchaseKeys; // 24
    public IntPtr GetFileDetails;               // 25
    public IntPtr GetLaunchCommandLine;         // 26
    public IntPtr IsSubscribedFromFamilySharing; // 27
}

/// <summary>
/// Wrapper for ISteamApps008 interface.
/// </summary>
public class SteamApps008 : NativeWrapper<ISteamApps008>
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeIsSubscribedApp(IntPtr self, uint appId);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr NativeGetCurrentGameLanguage(IntPtr self);

    public bool IsSubscribedApp(uint appId)
    {
        return Call<bool, NativeIsSubscribedApp>(Functions.IsSubscribedApp, appId);
    }

    public string GetCurrentGameLanguage()
    {
        var result = Call<IntPtr, NativeGetCurrentGameLanguage>(Functions.GetCurrentGameLanguage);
        return NativeStrings.PointerToString(result);
    }
}