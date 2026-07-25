using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// Vtable layout for ISteamUser012 interface (matches SAM/gibbed).
/// Used to get the logged-in user's SteamID.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ISteamUser012
{
    public IntPtr GetHSteamUser;          // 0
    public IntPtr LoggedOn;                // 1
    public IntPtr GetSteamID;             // 2
    public IntPtr InitiateGameConnection;  // 3
    public IntPtr TerminateGameConnection; // 4
    public IntPtr TrackAppUsageEvent;      // 5
    public IntPtr GetUserDataFolder;       // 6
    public IntPtr StartVoiceRecording;     // 7
    public IntPtr StopVoiceRecording;      // 8
    public IntPtr GetCompressedVoice;     // 9
    public IntPtr DecompressVoice;        // 10
    public IntPtr GetAuthSessionTicket;    // 11
    public IntPtr BeginAuthSession;       // 12
    public IntPtr EndAuthSession;         // 13
    public IntPtr CancelAuthTicket;       // 14
    public IntPtr UserHasLicenseForApp;   // 15
}

/// <summary>
/// Wrapper for ISteamUser012 interface.
/// Provides access to the logged-in user's identity.
/// </summary>
public class SteamUser012 : NativeWrapper<ISteamUser012>
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeLoggedOn(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void NativeGetSteamId(IntPtr self, out ulong steamId);

    public bool IsLoggedIn()
    {
        return Call<bool, NativeLoggedOn>(Functions.LoggedOn);
    }

    public ulong GetSteamId()
    {
        var del = GetFunction<NativeGetSteamId>(Functions.GetSteamID);
        del(ObjectAddress, out ulong steamId);
        return steamId;
    }
}
