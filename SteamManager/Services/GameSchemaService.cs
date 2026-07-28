using System.IO;
using SteamManager.Models;
using SteamManager.Steam;

namespace SteamManager.Services;

public class GameSchemaService
{
    private readonly string _steamInstallPath;

    public GameSchemaService(string steamInstallPath)
    {
        _steamInstallPath = steamInstallPath;
    }

    public (List<SchemaAchievementDefinition> Achievements, List<SchemaStatDefinition> Stats) LoadSchema(uint appId, string language = "english")
    {
        string schemaPath = Path.Combine(
            _steamInstallPath,
            "appcache",
            "stats",
            $"UserGameStatsSchema_{appId}.bin");

        var kv = KeyValue.LoadAsBinary(schemaPath);
        if (kv == null)
        {
            return (new List<SchemaAchievementDefinition>(), new List<SchemaStatDefinition>());
        }

        var achievements = new List<SchemaAchievementDefinition>();
        var stats = new List<SchemaStatDefinition>();

        var statsNode = kv[appId.ToString()]["stats"];
        if (statsNode.Valid == false || statsNode.Children == null)
        {
            return (achievements, stats);
        }

        foreach (var statNode in statsNode.Children)
        {
            if (statNode.Valid == false)
            {
                continue;
            }

            var statType = ParseStatType(statNode);
            var displayName = GetLocalizedString(statNode["display"]["name"], language, statNode["name"].AsString(""));

            switch (statType)
            {
                case UserStatType.Integer:
                {
                    var def = new SchemaIntegerStatDefinition
                    {
                        Id = statNode["name"].AsString(""),
                        DisplayName = displayName,
                        MinValue = statNode["min"].AsInteger(int.MinValue),
                        MaxValue = statNode["max"].AsInteger(int.MaxValue),
                        MaxChange = statNode["maxchange"].AsInteger(0),
                        IncrementOnly = statNode["incrementonly"].AsBoolean(false),
                        SetByTrustedGameServer = statNode["bSetByTrustedGS"].AsBoolean(false),
                        DefaultValue = statNode["default"].AsInteger(0),
                        Permission = statNode["permission"].AsInteger(0),
                    };
                    stats.Add(def);
                    break;
                }

                case UserStatType.Float:
                case UserStatType.AverageRate:
                {
                    var def = new SchemaFloatStatDefinition
                    {
                        Id = statNode["name"].AsString(""),
                        DisplayName = displayName,
                        MinValue = statNode["min"].AsFloat(float.MinValue),
                        MaxValue = statNode["max"].AsFloat(float.MaxValue),
                        MaxChange = statNode["maxchange"].AsFloat(0),
                        IncrementOnly = statNode["incrementonly"].AsBoolean(false),
                        DefaultValue = statNode["default"].AsFloat(0),
                        Permission = statNode["permission"].AsInteger(0),
                    };
                    stats.Add(def);
                    break;
                }

                case UserStatType.Achievements:
                case UserStatType.GroupAchievements:
                {
                    if (statNode.Children != null)
                    {
                        foreach (var bits in statNode.Children.Where(
                            b => string.Compare(b.Name, "bits", StringComparison.InvariantCultureIgnoreCase) == 0))
                        {
                            if (bits.Valid == false || bits.Children == null)
                            {
                                continue;
                            }

                            foreach (var bit in bits.Children)
                            {
                                var achievementDef = new SchemaAchievementDefinition
                                {
                                    Id = bit["name"].AsString(""),
                                    Name = GetLocalizedString(bit["display"]["name"], language, bit["name"].AsString("")),
                                    Description = GetLocalizedString(bit["display"]["desc"], language, ""),
                                    IconNormal = bit["display"]["icon"].AsString(""),
                                    IconLocked = bit["display"]["icon_gray"].AsString(""),
                                    IsHidden = bit["display"]["hidden"].AsBoolean(false),
                                    Permission = bit["permission"].AsInteger(0),
                                };
                                achievements.Add(achievementDef);
                            }
                        }
                    }
                    break;
                }
            }
        }

        return (achievements, stats);
    }

    private static UserStatType ParseStatType(KeyValue statNode)
    {
        var typeNode = statNode["type"];
        if (typeNode.Valid == true && typeNode.Type == KeyValueType.String)
        {
            if (Enum.TryParse((string)typeNode.Value, true, out UserStatType type))
            {
                return type;
            }
            return UserStatType.Invalid;
        }

        var typeIntNode = statNode["type_int"];
        var rawType = typeIntNode.Valid == true
            ? typeIntNode.AsInteger(0)
            : typeNode.AsInteger(0);
        return (UserStatType)rawType;
    }

    private static string GetLocalizedString(KeyValue kv, string language, string defaultValue)
    {
        var name = kv[language].AsString("");
        if (string.IsNullOrEmpty(name) == false)
        {
            return name;
        }

        if (language != "english")
        {
            name = kv["english"].AsString("");
            if (string.IsNullOrEmpty(name) == false)
            {
                return name;
            }
        }

        name = kv.AsString("");
        if (string.IsNullOrEmpty(name) == false)
        {
            return name;
        }

        return defaultValue;
    }
}
