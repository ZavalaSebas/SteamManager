# Attributions

Third-party licenses and copyright notices for code used in SteamManager.

---

## SAM.API / SAM.Game (Ported Code)

Portions of this software are derived from [Gibbed's Steam Achievement Manager (SAM)](https://github.com/gibbed/SteamAchievementManager).

### License (zlib)

```text
zlib License

Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would
   be appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not
   be misrepresented as being the original software.

3. This notice may not be removed or altered from any source
   distribution.
```

### Ported Files

| Original Location | Destination | Purpose |
|-------------------|-------------|---------|
| `SAM.Game/KeyValueType.cs` | `SteamManager/Steam/KeyValueType.cs` | Key-value binary format type enum |
| `SAM.Game/StreamHelpers.cs` | `SteamManager/Steam/KeyValueSerializer.cs` | Binary reading helpers (ReadValueU8, ReadStringUnicode, etc.) |
| `SAM.Game/KeyValue.cs` | `SteamManager/Steam/KeyValue.cs` | Binary Key-Value parser for UserGameStatsSchema_{appId}.bin |
| `SAM.Game/Stats/StatDefinition.cs` | `SteamManager/Models/SchemaStatDefinition.cs` | Stat definition model (Id, DisplayName, Permission) |
| `SAM.Game/Stats/IntegerStatDefinition.cs` | `SteamManager/Models/SchemaIntegerStatDefinition.cs` | Integer stat metadata (min/max, incrementonly, etc.) |
| `SAM.Game/Stats/FloatStatDefinition.cs` | `SteamManager/Models/SchemaFloatStatDefinition.cs` | Float stat metadata |
| `SAM.Game/Stats/AchievementDefinition.cs` | `SteamManager/Models/SchemaAchievementDefinition.cs` | Achievement definition with Permission field |
| `SAM.API/Types/UserStatType.cs` | `SteamManager/Steam/UserStatType.cs` | Stat type enum (Integer, Float, Achievements, etc.) |

### Purpose

The Key-Value binary parser is used to read `UserGameStatsSchema_{appId}.bin` files from Steam's appcache directory. These files contain the official schema for each game's achievements and statistics, including:
- Achievement names, descriptions, icons
- Achievement `Permission` flags (which indicate protected/locked achievements)
- Stat definitions with min/max bounds and display names
- Localized strings for the user's Steam language

This enables SteamManager to properly identify protected achievements and provide accurate stat metadata, replacing the hardcoded `GameStats.cs` approach.
