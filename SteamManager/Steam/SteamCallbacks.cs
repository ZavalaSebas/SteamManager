using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// Callback parameter structs matching the Steam API memory layout.
/// </summary>
public static class SteamCallbackStructs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UserStatsReceived_t
    {
        public ulong GameId;
        public EResult Result;
        public ulong SteamIdUser;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UserStatsStored_t
    {
        public ulong GameId;
        public EResult Result;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UserAchievementStored_t
    {
        public ulong GameId;
        [MarshalAs(UnmanagedType.I1)]
        public bool GroupAchievement;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string AchievementName;
        public uint CurProgress;
        public uint MaxProgress;
    }
}

public enum EResult
{
    OK = 1,
    Fail = 2,
    NoConnection = 3,
    InvalidPassword = 5,
    LoggedInElsewhere = 6,
    InvalidProtocolVer = 7,
    BadParam = 8,
    FileNotFound = 9,
    Busy = 10,
    InvalidState = 11,
    InvalidParameter = 12,
    Timeout = 13,
    NotLoggedOn = 14,
    InvalidSteamUser = 15,
    Banned = 16,
    ServiceUnavailable = 17,
    NotLoggedOnAnymore = 18,
    Rerun = 19,
    InvalidCEGSubmission = 24,
    RestrictedDevice = 25,
    Blocked = 27,
}
