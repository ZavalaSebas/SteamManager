/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 * Ported from SAM.Game/Stats/AchievementDefinition.cs
 * See ATTRIBUTIONS.md for license details.
 */

namespace SteamManager.Models;

public class SchemaAchievementDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconNormal { get; set; } = string.Empty;
    public string IconLocked { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public int Permission { get; set; }

    public override string ToString()
    {
        return $"{this.Name ?? this.Id ?? base.ToString()}: {this.Permission}";
    }
}
