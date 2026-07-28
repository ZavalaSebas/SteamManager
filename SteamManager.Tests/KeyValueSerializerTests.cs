using System.IO;
using SteamManager.Steam;
using static SteamManager.Steam.KeyValueSerializer;

namespace SteamManager.Tests;

public class KeyValueSerializerTests
{
    [Fact]
    public void ReadValueU8_ReturnsCorrectByte()
    {
        var data = new byte[] { 0x42 };
        using var stream = new MemoryStream(data);
        var result = stream.ReadValueU8();
        Assert.Equal(0x42, result);
    }

    [Fact]
    public void ReadValueS32_ReturnsCorrectInt32()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using var stream = new MemoryStream(data);
        var result = stream.ReadValueS32();
        Assert.Equal(0x04030201, result);
    }

    [Fact]
    public void ReadValueU32_ReturnsCorrectUInt32()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using var stream = new MemoryStream(data);
        var result = stream.ReadValueU32();
        Assert.Equal(0x04030201u, result);
    }

    [Fact]
    public void ReadValueU64_ReturnsCorrectUInt64()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        using var stream = new MemoryStream(data);
        var result = stream.ReadValueU64();
        Assert.Equal(0x0807060504030201UL, result);
    }

    [Fact]
    public void ReadValueF32_ReturnsCorrectFloat()
    {
        var data = BitConverter.GetBytes(3.14159f);
        using var stream = new MemoryStream(data);
        var result = stream.ReadValueF32();
        Assert.Equal(3.14159f, result, 5);
    }

    [Fact]
    public void ReadStringUnicode_ReadStringTerminatedByNull()
    {
        var data = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x00 };
        using var stream = new MemoryStream(data);
        var result = stream.ReadStringUnicode();
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ReadStringUnicode_ReturnsEmptyForEmptyString()
    {
        var data = new byte[] { 0x00 };
        using var stream = new MemoryStream(data);
        var result = stream.ReadStringUnicode();
        Assert.Equal("", result);
    }
}
