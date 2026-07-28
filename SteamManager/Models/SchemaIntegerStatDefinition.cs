/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 * Ported from SAM.Game/Stats/IntegerStatDefinition.cs
 * See ATTRIBUTIONS.md for license details.
 */

namespace SteamManager.Models;

public class SchemaIntegerStatDefinition : SchemaStatDefinition
{
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public int MaxChange { get; set; }
    public bool IncrementOnly { get; set; }
    public bool SetByTrustedGameServer { get; set; }
    public int DefaultValue { get; set; }
}
