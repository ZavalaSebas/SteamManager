/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 * Ported from SAM.Game/Stats/StatDefinition.cs
 * See ATTRIBUTIONS.md for license details.
 */

namespace SteamManager.Models;

public abstract class SchemaStatDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Permission { get; set; }
}
