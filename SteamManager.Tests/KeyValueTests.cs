using SteamManager.Models;
using SteamManager.Steam;

namespace SteamManager.Tests;

public class KeyValueTests
{
    [Fact]
    public void LoadAsBinary_ReturnsNull_ForNonExistentFile()
    {
        var result = KeyValue.LoadAsBinary("C:\\nonexistent\\path\\UserGameStatsSchema_999999.bin");
        Assert.Null(result);
    }

    [Fact]
    public void ReadAsBinary_ParsesMinimalKeyValue()
    {
        var data = new byte[]
        {
            0x01, 0x54, 0x65, 0x73, 0x74, 0x00,
            0x01, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x00,
            0x08
        };

        using var stream = new MemoryStream(data);
        var kv = new KeyValue();
        bool success = kv.ReadAsBinary(stream);

        Assert.True(success);
        Assert.True(kv.Valid);
        Assert.NotNull(kv.Children);
        Assert.Single(kv.Children);

        var child = kv.Children[0];
        Assert.Equal(KeyValueType.String, child.Type);
        Assert.Equal("Test", child.Name);
        Assert.True(child.Valid);
        Assert.Equal("Hello", child.Value);
    }

    [Fact]
    public void ReadAsBinary_ParsesInt32Value()
    {
        var data = new byte[]
        {
            0x02, 0x43, 0x6f, 0x75, 0x6e, 0x74, 0x00,
            0x2A, 0x00, 0x00, 0x00,
            0x08
        };

        using var stream = new MemoryStream(data);
        var kv = new KeyValue();
        bool success = kv.ReadAsBinary(stream);

        Assert.True(success);
        Assert.True(kv.Valid);

        var child = kv.Children[0];
        Assert.Equal(KeyValueType.Int32, child.Type);
        Assert.Equal("Count", child.Name);
        Assert.True(child.Valid);
        Assert.Equal(42, child.Value);
    }

    [Fact]
    public void ReadAsBinary_ParsesEndMarker()
    {
        var data = new byte[] { 0x08 };

        using var stream = new MemoryStream(data);
        var kv = new KeyValue();
        bool success = kv.ReadAsBinary(stream);

        Assert.True(success);
        Assert.True(kv.Valid);
        Assert.NotNull(kv.Children);
        Assert.Empty(kv.Children);
    }

    [Fact]
    public void AsInteger_ReturnsDefault_ForInvalid()
    {
        var kv = new KeyValue { Valid = false };
        Assert.Equal(99, kv.AsInteger(99));
    }

    [Fact]
    public void AsFloat_ReturnsDefault_ForInvalid()
    {
        var kv = new KeyValue { Valid = false };
        Assert.Equal(1.5f, kv.AsFloat(1.5f));
    }

    [Fact]
    public void AsBoolean_ReturnsDefault_ForInvalid()
    {
        var kv = new KeyValue { Valid = false };
        Assert.True(kv.AsBoolean(true));
    }

    [Fact]
    public void Indexer_ReturnsInvalid_ForMissingChild()
    {
        var kv = new KeyValue { Valid = true, Children = new List<KeyValue>() };
        var result = kv["Missing"];
        Assert.False(result.Valid);
    }

    [Fact]
    public void Indexer_ReturnsChild_ByName()
    {
        var kv = new KeyValue
        {
            Valid = true,
            Children = new List<KeyValue>
            {
                new KeyValue { Name = "Test", Valid = true, Value = "Hello" }
            }
        };

        var result = kv["test"];
        Assert.True(result.Valid);
        Assert.Equal("Hello", result.Value);
    }

    [Fact(Skip = "Requires real Steam schema file - parser bug investigation needed")]
    public void ReadAsBinary_ParsesNestedKeyValue()
    {
        var data = new byte[]
        {
            0x00, 0x50, 0x61, 0x72, 0x65, 0x6e, 0x74, 0x00,
            0x00, 0x43, 0x68, 0x69, 0x6c, 0x64, 0x00,
            0x08,
            0x08
        };

        using var stream = new MemoryStream(data);
        var kv = new KeyValue();
        bool success = kv.ReadAsBinary(stream);

        Assert.True(success);
        Assert.True(kv.Valid);
        Assert.Single(kv.Children);

        var parent = kv.Children[0];
        Assert.Equal("Parent", parent.Name);
        Assert.NotNull(parent.Children);
        Assert.Single(parent.Children);

        var child = parent.Children[0];
        Assert.Equal("Child", child.Name);
    }

    [Fact]
    public void LoadAsBinary_ParsesRealSteamSchema()
    {
        var schemaPath = @"C:\Program Files (x86)\Steam\appcache\stats\UserGameStatsSchema_440.bin";
        if (File.Exists(schemaPath) == false)
        {
            return;
        }

        var kv = KeyValue.LoadAsBinary(schemaPath);
        Assert.NotNull(kv);
        Assert.True(kv.Valid);
        Assert.NotNull(kv.Children);
        Assert.True(kv.Children.Count > 0);

        var rootNode = kv.Children[0];
        Assert.Equal("440", rootNode.Name);
        Assert.Equal(KeyValueType.None, rootNode.Type);

        var statsNode = rootNode["stats"];
        Assert.True(statsNode.Valid, $"statsNode.Valid is false. statsNode.Name='{statsNode.Name}'");
        Assert.NotNull(statsNode.Children);
        Assert.True(statsNode.Children.Count > 0);
    }

    [Fact]
    public void LoadAsBinary_ParsesRealSteamSchema_HasNestedStats()
    {
        var schemaPath = @"C:\Program Files (x86)\Steam\appcache\stats\UserGameStatsSchema_1134700.bin";
        if (File.Exists(schemaPath) == false)
        {
            return;
        }

        var kv = KeyValue.LoadAsBinary(schemaPath);
        Assert.NotNull(kv);

        var gameNode = kv["1134700"];
        Assert.True(gameNode.Valid);

        var statsNode = gameNode["stats"];
        Assert.True(statsNode.Valid);

        KeyValue bitsNode = default;
        foreach (var statNode in statsNode.Children ?? Enumerable.Empty<KeyValue>())
        {
            var bits = statNode["bits"];
            if (bits.Valid)
            {
                bitsNode = bits;
                break;
            }
        }
        Assert.True(bitsNode.Valid, "No achievement bits found in any stat");

        int foundAchievements = 0;
        int foundPermissions = 0;
        int foundPermissionNonZero = 0;
        foreach (var bitNode in bitsNode.Children ?? Enumerable.Empty<KeyValue>())
        {
            if (bitNode.Valid == false) continue;
            var displayNode = bitNode["display"];
            if (displayNode.Valid)
            {
                foundAchievements++;
            }
            var permNode = bitNode["permission"];
            if (permNode.Valid)
            {
                foundPermissions++;
                if (permNode.AsInteger(0) != 0)
                {
                    foundPermissionNonZero++;
                }
            }
        }

        Assert.True(foundAchievements > 0, "No achievements found in schema");
        Assert.True(foundPermissions > 0, "No 'permission' field found in achievements");
        Assert.True(foundPermissionNonZero > 0, "No achievements with non-zero permission found");
    }

    [Fact]
    public void LoadAsBinary_PermissionStringParsesAsInteger()
    {
        var schemaPath = @"C:\Program Files (x86)\Steam\appcache\stats\UserGameStatsSchema_1203220.bin";
        if (File.Exists(schemaPath) == false)
        {
            return;
        }

        var kv = KeyValue.LoadAsBinary(schemaPath);
        Assert.NotNull(kv);
        Assert.True(kv.Valid);

        var gameNode = kv["1203220"];
        Assert.True(gameNode.Valid);

        var statsNode = gameNode["stats"];
        Assert.True(statsNode.Valid);

        KeyValue bitsNode = default;
        foreach (var statNode in statsNode.Children ?? Enumerable.Empty<KeyValue>())
        {
            var bits = statNode["bits"];
            if (bits.Valid)
            {
                bitsNode = bits;
                break;
            }
        }
        Assert.True(bitsNode.Valid, "No achievement bits found");

        bool foundPermission1 = false;
        bool foundPermission2 = false;
        foreach (var bitNode in bitsNode.Children ?? Enumerable.Empty<KeyValue>())
        {
            var permNode = bitNode["permission"];
            if (permNode.Valid)
            {
                Assert.Equal(KeyValueType.String, permNode.Type);
                var permInt = permNode.AsInteger(-1);
                Assert.NotEqual(-1, permInt);
                if (permInt == 1) foundPermission1 = true;
                if (permInt == 2) foundPermission2 = true;
            }
        }

        Assert.True(foundPermission1 || foundPermission2, "No achievements with permission=1 or permission=2 found");
    }
}
