using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// Vtable layout for ISteamClient018 interface (matches SAM/gibbed).
/// Fields are function pointers in vtable order.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ISteamClient018
{
    public IntPtr CreateSteamPipe;            // 0
    public IntPtr ReleaseSteamPipe;           // 1
    public IntPtr ConnectToGlobalUser;        // 2
    public IntPtr CreateLocalUser;             // 3
    public IntPtr ReleaseUser;                 // 4
    public IntPtr GetISteamUser;               // 5
    public IntPtr GetISteamGameServer;         // 6
    public IntPtr SetLocalIPBinding;           // 7
    public IntPtr GetISteamFriends;            // 8
    public IntPtr GetISteamUtils;              // 9
    public IntPtr GetISteamMatchmaking;        // 10
    public IntPtr GetISteamMatchmakingServers; // 11
    public IntPtr GetISteamGenericInterface;   // 12
    public IntPtr GetISteamUserStats;          // 13
    public IntPtr GetISteamGameServerStats;    // 14
    public IntPtr GetISteamApps;               // 15
    public IntPtr GetISteamNetworking;         // 16
    public IntPtr GetISteamRemoteStorage;      // 17
    public IntPtr GetISteamScreenshots;        // 18
    public IntPtr GetISteamGameSearch;         // 19
    public IntPtr RunFrame;                    // 20
    public IntPtr GetIPCCallCount;             // 21
    public IntPtr SetWarningMessageHook;       // 22
    public IntPtr ShutdownIfAllPipesClosed;    // 23
    public IntPtr GetISteamHTTP;               // 24
    public IntPtr DEPRECATED_GetISteamUnifiedMessages; // 25
    public IntPtr GetISteamController;         // 26
    public IntPtr GetISteamUGC;                // 27
    public IntPtr GetISteamAppList;            // 28
    public IntPtr GetISteamMusic;              // 29
    public IntPtr GetISteamMusicRemote;         // 30
    public IntPtr GetISteamHTMLSurface;        // 31
    public IntPtr DEPRECATED_Set_SteamAPI_CPostAPIResultInProcess; // 32
    public IntPtr DEPRECATED_Remove_SteamAPI_CPostAPIResultInProcess; // 33
    public IntPtr Set_SteamAPI_CCheckCallbackRegisteredInProcess; // 34
    public IntPtr GetISteamInventory;          // 35
    public IntPtr GetISteamVideo;              // 36
    public IntPtr GetISteamParentalSettings;   // 37
    public IntPtr GetISteamInput;              // 38
    public IntPtr GetISteamParties;            // 39
}

/// <summary>
/// Wrapper for ISteamClient018 interface.
/// Provides methods to create pipes, connect users, and get other interfaces.
/// </summary>
public class SteamClient018 : NativeWrapper<ISteamClient018>
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int NativeCreateSteamPipe(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeReleaseSteamPipe(IntPtr self, int pipe);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int NativeConnectToGlobalUser(IntPtr self, int pipe);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void NativeReleaseUser(IntPtr self, int pipe, int user);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr NativeGetISteamUserStats(IntPtr self, int user, int pipe, IntPtr version);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr NativeGetISteamApps(IntPtr self, int user, int pipe, IntPtr version);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr NativeGetISteamUtils(IntPtr self, int pipe, IntPtr version);

    public int CreateSteamPipe()
    {
        return Call<int, NativeCreateSteamPipe>(Functions.CreateSteamPipe);
    }

    public bool ReleaseSteamPipe(int pipe)
    {
        return Call<bool, NativeReleaseSteamPipe>(Functions.ReleaseSteamPipe, pipe);
    }

    public int ConnectToGlobalUser(int pipe)
    {
        return Call<int, NativeConnectToGlobalUser>(Functions.ConnectToGlobalUser, pipe);
    }

    public void ReleaseUser(int pipe, int user)
    {
        Call<NativeReleaseUser>(Functions.ReleaseUser, pipe, user);
    }

    public IntPtr GetISteamUserStats(int user, int pipe, string version)
    {
        using var nativeVersion = NativeStrings.StringToStringHandle(version);
        return Call<IntPtr, NativeGetISteamUserStats>(
            Functions.GetISteamUserStats, user, pipe, nativeVersion.DangerousGetHandle());
    }

    public IntPtr GetISteamApps(int user, int pipe, string version)
    {
        using var nativeVersion = NativeStrings.StringToStringHandle(version);
        return Call<IntPtr, NativeGetISteamApps>(
            Functions.GetISteamApps, user, pipe, nativeVersion.DangerousGetHandle());
    }

    public IntPtr GetISteamUtils(int pipe, string version)
    {
        using var nativeVersion = NativeStrings.StringToStringHandle(version);
        return Call<IntPtr, NativeGetISteamUtils>(
            Functions.GetISteamUtils, pipe, nativeVersion.DangerousGetHandle());
    }
}