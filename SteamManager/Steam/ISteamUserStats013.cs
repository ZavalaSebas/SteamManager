using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// Vtable layout for ISteamUserStats013 interface (matches SAM/gibbed).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ISteamUserStats013
{
    public IntPtr GetStatFloat;                  // 0
    public IntPtr GetStatInteger;                // 1
    public IntPtr SetStatFloat;                  // 2
    public IntPtr SetStatInteger;                // 3
    public IntPtr UpdateAvgRateStat;             // 4
    public IntPtr GetAchievement;                // 5
    public IntPtr SetAchievement;                // 6
    public IntPtr ClearAchievement;              // 7
    public IntPtr GetAchievementAndUnlockTime;   // 8
    public IntPtr StoreStats;                    // 9
    public IntPtr GetAchievementIcon;            // 10
    public IntPtr GetAchievementDisplayAttribute;// 11
    public IntPtr IndicateAchievementProgress;   // 12
    public IntPtr GetNumAchievements;             // 13
    public IntPtr GetAchievementName;            // 14
    public IntPtr RequestUserStats;              // 15
    public IntPtr GetUserStatFloat;               // 16
    public IntPtr GetUserStatInt;                 // 17
    public IntPtr GetUserAchievement;             // 18
    public IntPtr GetUserAchievementAndUnlockTime;// 19
    public IntPtr ResetAllStats;                  // 20
    public IntPtr FindOrCreateLeaderboard;        // 21
    public IntPtr FindLeaderboard;                // 22
    public IntPtr GetLeaderboardName;             // 23
    public IntPtr GetLeaderboardEntryCount;       // 24
    public IntPtr GetLeaderboardSortMethod;       // 25
    public IntPtr GetLeaderboardDisplayType;      // 26
    public IntPtr DownloadLeaderboardEntries;     // 27
    public IntPtr DownloadLeaderboardEntriesForUsers; // 28
    public IntPtr GetDownloadedLeaderboardEntry;  // 29
    public IntPtr UploadLeaderboardScore;         // 30
    public IntPtr AttachLeaderboardUGC;           // 31
    public IntPtr GetNumberOfCurrentPlayers;      // 32
    public IntPtr RequestGlobalAchievementPercentages; // 33
    public IntPtr GetMostAchievedAchievementInfo; // 34
    public IntPtr GetNextMostAchievedAchievementInfo; // 35
    public IntPtr GetAchievementAchievedPercent;   // 36
    public IntPtr RequestGlobalStats;             // 37
    public IntPtr GetGlobalStatFloat;             // 38
    public IntPtr GetGlobalStatInteger;           // 39
    public IntPtr GetGlobalStatHistoryFloat;      // 40
    public IntPtr GetGlobalStatHistoryInteger;    // 41
    public IntPtr GetAchievementProgressLimitsFloat; // 42
    public IntPtr GetAchievementProgressLimitsInteger; // 43
}

/// <summary>
/// Wrapper for ISteamUserStats013 interface.
/// Provides methods to read/write stats and achievements.
/// </summary>
public class SteamUserStats013 : NativeWrapper<ISteamUserStats013>
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeGetStatFloat(IntPtr self, IntPtr name, out float data);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeGetStatInt(IntPtr self, IntPtr name, out int data);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeSetStatFloat(IntPtr self, IntPtr name, float data);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeSetStatInt(IntPtr self, IntPtr name, int data);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeGetAchievement(IntPtr self, IntPtr name,
        [MarshalAs(UnmanagedType.I1)] out bool achieved);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeSetAchievement(IntPtr self, IntPtr name);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeClearAchievement(IntPtr self, IntPtr name);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeGetAchievementAndUnlockTime(IntPtr self, IntPtr name,
        [MarshalAs(UnmanagedType.I1)] out bool achieved, out uint unlockTime);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeStoreStats(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int NativeGetAchievementIcon(IntPtr self, IntPtr name);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr NativeGetAchievementDisplayAttribute(IntPtr self, IntPtr name, IntPtr key);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeIndicateAchievementProgress(IntPtr self, IntPtr name, uint cur, uint max);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate uint NativeGetNumAchievements(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr NativeGetAchievementName(IntPtr self, uint index);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NativeResetAllStats(IntPtr self,
        [MarshalAs(UnmanagedType.I1)] bool achievementsToo);

    public bool GetStat(string name, out float value)
    {
        using var nameHandle = NativeStrings.StringToStringHandle(name);
        var del = GetFunction<NativeGetStatFloat>(Functions.GetStatFloat);
        return del(ObjectAddress, nameHandle.DangerousGetHandle(), out value);
    }

    public bool GetStat(string name, out int value)
    {
        using var nameHandle = NativeStrings.StringToStringHandle(name);
        var del = GetFunction<NativeGetStatInt>(Functions.GetStatInteger);
        return del(ObjectAddress, nameHandle.DangerousGetHandle(), out value);
    }

    public bool SetStat(string name, float value)
    {
        using var nameHandle = NativeStrings.StringToStringHandle(name);
        var del = GetFunction<NativeSetStatFloat>(Functions.SetStatFloat);
        return del(ObjectAddress, nameHandle.DangerousGetHandle(), value);
    }

    public bool SetStat(string name, int value)
    {
        using var nameHandle = NativeStrings.StringToStringHandle(name);
        var del = GetFunction<NativeSetStatInt>(Functions.SetStatInteger);
        return del(ObjectAddress, nameHandle.DangerousGetHandle(), value);
    }

    public bool GetAchievement(string name, out bool achieved)
    {
        using var nameHandle = NativeStrings.StringToStringHandle(name);
        var del = GetFunction<NativeGetAchievement>(Functions.GetAchievement);
        return del(ObjectAddress, nameHandle.DangerousGetHandle(), out achieved);
    }

    public bool SetAchievement(string name)
    {
        using var nameHandle = NativeStrings.StringToStringHandle(name);
        var del = GetFunction<NativeSetAchievement>(Functions.SetAchievement);
        return del(ObjectAddress, nameHandle.DangerousGetHandle());
    }

    public bool ClearAchievement(string name)
    {
        using var nameHandle = NativeStrings.StringToStringHandle(name);
        var del = GetFunction<NativeClearAchievement>(Functions.ClearAchievement);
        return del(ObjectAddress, nameHandle.DangerousGetHandle());
    }

    public bool GetAchievementAndUnlockTime(string name, out bool achieved, out uint unlockTime)
    {
        using var nameHandle = NativeStrings.StringToStringHandle(name);
        var del = GetFunction<NativeGetAchievementAndUnlockTime>(Functions.GetAchievementAndUnlockTime);
        return del(ObjectAddress, nameHandle.DangerousGetHandle(), out achieved, out unlockTime);
    }

    public bool StoreStats()
    {
        var del = GetFunction<NativeStoreStats>(Functions.StoreStats);
        return del(ObjectAddress);
    }

    public int GetAchievementIcon(string name)
    {
        using var nameHandle = NativeStrings.StringToStringHandle(name);
        var del = GetFunction<NativeGetAchievementIcon>(Functions.GetAchievementIcon);
        return del(ObjectAddress, nameHandle.DangerousGetHandle());
    }

    public string GetAchievementDisplayAttribute(string name, string key)
    {
        using var nameHandle = NativeStrings.StringToStringHandle(name);
        using var keyHandle = NativeStrings.StringToStringHandle(key);
        var del = GetFunction<NativeGetAchievementDisplayAttribute>(Functions.GetAchievementDisplayAttribute);
        IntPtr result = del(ObjectAddress, nameHandle.DangerousGetHandle(), keyHandle.DangerousGetHandle());
        return NativeStrings.PointerToString(result);
    }

    public uint GetNumAchievements()
    {
        var del = GetFunction<NativeGetNumAchievements>(Functions.GetNumAchievements);
        return del(ObjectAddress);
    }

    public string GetAchievementName(uint index)
    {
        var del = GetFunction<NativeGetAchievementName>(Functions.GetAchievementName);
        IntPtr result = del(ObjectAddress, index);
        return NativeStrings.PointerToString(result);
    }

    public bool IndicateAchievementProgress(string name, uint cur, uint max)
    {
        using var nameHandle = NativeStrings.StringToStringHandle(name);
        var del = GetFunction<NativeIndicateAchievementProgress>(Functions.IndicateAchievementProgress);
        return del(ObjectAddress, nameHandle.DangerousGetHandle(), cur, max);
    }

    public bool ResetAllStats(bool achievementsToo)
    {
        var del = GetFunction<NativeResetAllStats>(Functions.ResetAllStats);
        return del(ObjectAddress, achievementsToo);
    }
}