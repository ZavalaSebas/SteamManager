/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 * Ported from SAM.Game/Stats/FloatStatDefinition.cs
 * See ATTRIBUTIONS.md for license details.
 */

namespace SteamManager.Models;

public class SchemaFloatStatDefinition : SchemaStatDefinition
{
    public float MinValue { get; set; }
    public float MaxValue { get; set; }
    public float MaxChange { get; set; }
    public bool IncrementOnly { get; set; }
    public float DefaultValue { get; set; }
}
