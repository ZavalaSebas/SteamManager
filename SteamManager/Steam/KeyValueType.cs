/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 * Ported from SAM.Game/KeyValueType.cs
 * See ATTRIBUTIONS.md for license details.
 */

namespace SteamManager.Steam;

public enum KeyValueType : byte
{
    None = 0,
    String = 1,
    Int32 = 2,
    Float32 = 3,
    Pointer = 4,
    WideString = 5,
    Color = 6,
    UInt64 = 7,
    End = 8,
}
