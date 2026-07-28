using SteamManager.Models;

namespace SteamManager.Tests;

public class SchemaModelTests
{
    [Fact]
    public void SchemaAchievementDefinition_DefaultValues()
    {
        var def = new SchemaAchievementDefinition();
        Assert.Equal(string.Empty, def.Id);
        Assert.Equal(string.Empty, def.Name);
        Assert.Equal(string.Empty, def.Description);
        Assert.Equal(string.Empty, def.IconNormal);
        Assert.Equal(string.Empty, def.IconLocked);
        Assert.False(def.IsHidden);
        Assert.Equal(0, def.Permission);
    }

    [Fact]
    public void SchemaAchievementDefinition_ToString_ShowsNameAndPermission()
    {
        var def = new SchemaAchievementDefinition
        {
            Id = "ACH_TEST",
            Name = "Test Achievement",
            Permission = 1
        };

        var result = def.ToString();
        Assert.Contains("Test Achievement", result);
        Assert.Contains("1", result);
    }

    [Fact]
    public void SchemaAchievementDefinition_ToString_UsesId_WhenNameEmpty()
    {
        var def = new SchemaAchievementDefinition
        {
            Id = "ACH_TEST",
            Name = null,
            Permission = 0
        };

        var result = def.ToString();
        Assert.Contains("ACH_TEST", result);
        Assert.Contains("0", result);
    }

    [Fact]
    public void SchemaIntegerStatDefinition_DefaultValues()
    {
        var def = new SchemaIntegerStatDefinition();
        Assert.Equal(0, def.MinValue);
        Assert.Equal(0, def.MaxValue);
        Assert.Equal(0, def.MaxChange);
        Assert.False(def.IncrementOnly);
        Assert.False(def.SetByTrustedGameServer);
        Assert.Equal(0, def.DefaultValue);
        Assert.Equal(0, def.Permission);
    }

    [Fact]
    public void SchemaFloatStatDefinition_DefaultValues()
    {
        var def = new SchemaFloatStatDefinition();
        Assert.Equal(0f, def.MinValue);
        Assert.Equal(0f, def.MaxValue);
        Assert.Equal(0f, def.MaxChange);
        Assert.False(def.IncrementOnly);
        Assert.Equal(0f, def.DefaultValue);
        Assert.Equal(0, def.Permission);
    }

    [Fact]
    public void SchemaStatDefinition_PermissionIsPublic()
    {
        var intDef = new SchemaIntegerStatDefinition { Permission = 3 };
        var floatDef = new SchemaFloatStatDefinition { Permission = 1 };
        var achDef = new SchemaAchievementDefinition { Permission = 2 };

        Assert.Equal(3, intDef.Permission);
        Assert.Equal(1, floatDef.Permission);
        Assert.Equal(2, achDef.Permission);
    }
}
