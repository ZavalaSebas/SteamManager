/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 * Ported from SAM.API/Types/UserStatType.cs
 * See ATTRIBUTIONS.md for license details.
 */

namespace SteamManager.Steam;

public enum UserStatType
{
    Invalid = 0,
    Integer = 1,
    Int = Integer,
    Float = 2,
    AverageRate = 3,
    Achievements = 4,
    GroupAchievements = 5,
}
