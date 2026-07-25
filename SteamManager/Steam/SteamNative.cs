using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// All P/Invoke declarations for steam_api64.dll.
/// This is the single source of truth for native interop.
/// </summary>
internal static partial class SteamNative
{
    private const string DllName = Config.SteamDll;

    #region Lifecycle

    [LibraryImport(DllName, EntryPoint = "SteamAPI_Init")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_Init();

    [LibraryImport(DllName, EntryPoint = "SteamAPI_Shutdown")]
    public static partial void SteamAPI_Shutdown();

    [LibraryImport(DllName, EntryPoint = "SteamAPI_RunCallbacks")]
    public static partial void SteamAPI_RunCallbacks();

    [LibraryImport(DllName, EntryPoint = "SteamAPI_RestartAppIfNecessary")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_RestartAppIfNecessary(uint unOwnAppID);

    #endregion

    #region UserStats - General

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_RequestCurrentStats")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_RequestCurrentStats(IntPtr instancePointer);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_StoreStats")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_StoreStats(IntPtr instancePointer);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_ResetAllStats")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_ResetAllStats(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.I1)] bool bAchievementsToo);

    #endregion

    #region UserStats - Stats

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_GetStat")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_GetStat(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.LPStr)] string pchName, ref int pData);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_GetStat")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_GetStat(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.LPStr)] string pchName, ref float pData);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_SetStat")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_SetStat(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.LPStr)] string pchName, int nData);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_SetStat")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_SetStat(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.LPStr)] string pchName, float fData);

    #endregion

    #region UserStats - Achievements

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_GetNumAchievements")]
    public static partial uint SteamAPI_ISteamUserStats_GetNumAchievements(IntPtr instancePointer);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_GetAchievementName")]
    public static partial IntPtr SteamAPI_ISteamUserStats_GetAchievementName(IntPtr instancePointer,
        uint iAchievement);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_GetAchievement")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_GetAchievement(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.LPStr)] string pchName,
        [MarshalAs(UnmanagedType.I1)] out bool pbAchieved);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_SetAchievement")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_SetAchievement(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.LPStr)] string pchName);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_ClearAchievement")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_ClearAchievement(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.LPStr)] string pchName);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_GetAchievementAndUnlockTime")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_GetAchievementAndUnlockTime(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.LPStr)] string pchName,
        [MarshalAs(UnmanagedType.I1)] out bool pbAchieved,
        out uint punUnlockTime);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_GetAchievementDisplayAttribute")]
    public static partial IntPtr SteamAPI_ISteamUserStats_GetAchievementDisplayAttribute(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.LPStr)] string pchName,
        [MarshalAs(UnmanagedType.LPStr)] string pchKey);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_GetAchievementIcon")]
    public static partial int SteamAPI_ISteamUserStats_GetAchievementIcon(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.LPStr)] string pchName);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_IndicateAchievementProgress")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUserStats_IndicateAchievementProgress(IntPtr instancePointer,
        [MarshalAs(UnmanagedType.LPStr)] string pchName,
        uint nCurProgress,
        uint nMaxProgress);

    #endregion

    #region Utils - Image decoding

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUtils_GetImageSize")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUtils_GetImageSize(IntPtr instancePointer,
        int iImage,
        out uint pnWidth,
        out uint pnHeight);

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUtils_GetImageRGBA")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamUtils_GetImageRGBA(IntPtr instancePointer,
        int iImage,
        byte[] pubDest,
        int nDestBufferSize);

    #endregion

    #region Apps

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamApps_IsSubscribedApp")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SteamAPI_ISteamApps_IsSubscribedApp(IntPtr instancePointer,
        uint appID);

    #endregion

    #region Internal - Interface getters

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUserStats_GetSteamUserStats")]
    public static partial IntPtr SteamAPI_ISteamUserStats_GetSteamUserStats();

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamUtils_GetSteamUtils")]
    public static partial IntPtr SteamAPI_ISteamUtils_GetSteamUtils();

    [LibraryImport(DllName, EntryPoint = "SteamAPI_ISteamApps_GetSteamApps")]
    public static partial IntPtr SteamAPI_ISteamApps_GetSteamApps();

    #endregion
}
