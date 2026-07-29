using System.IO;
using System.Xml.XPath;

namespace SteamManager.Tests;

public class XmlParsingTests
{
    [Fact]
    public void XPathDocument_ParsesAllGameElements_FromKnownXml()
    {
        var xml = "<?xml version=\"1.0\"?><games>" +
            "<game>100</game><game>200</game><game>300</game>" +
            "<game>400</game><game>500</game>" +
            "</games>";

        using var stringReader = new StringReader(xml);
        var document = new XPathDocument(stringReader);
        var navigator = document.CreateNavigator();
        var nodes = navigator.Select("/games/game");

        var appIds = new List<uint>();
        while (nodes.MoveNext())
        {
            if (uint.TryParse(nodes.Current.Value, out uint appId) && appId > 0)
                appIds.Add(appId);
        }

        Assert.Equal(5, appIds.Count);
        Assert.Equal(5, appIds.Distinct().Count());
    }

    [Fact]
    public void XPathDocument_DoesNotSkipElements_WhenIterating()
    {
        var appIds = new List<uint>();
        var xml = "<?xml version=\"1.0\"?><games>";
        for (int i = 0; i < 100; i++)
            xml += $"<game>{i + 1}</game>";
        xml += "</games>";

        using var stringReader = new StringReader(xml);
        var document = new XPathDocument(stringReader);
        var navigator = document.CreateNavigator();
        var nodes = navigator.Select("/games/game");

        while (nodes.MoveNext())
        {
            if (uint.TryParse(nodes.Current.Value, out uint appId) && appId > 0)
                appIds.Add(appId);
        }

        Assert.Equal(100, appIds.Count);
    }

    [Fact]
    public void XPathDocument_ParsesGameWithTypeAttribute()
    {
        var xml = "<?xml version=\"1.0\"?><games>" +
            "<game type=\"normal\">100</game>" +
            "<game type=\"demo\">200</game>" +
            "<game>300</game>" +
            "<game type=\"junk\">400</game>" +
            "<game type=\"mod\">500</game>" +
            "</games>";

        using var stringReader = new StringReader(xml);
        var document = new XPathDocument(stringReader);
        var navigator = document.CreateNavigator();
        var nodes = navigator.Select("/games/game");

        var appIds = new List<uint>();
        var types = new List<string>();
        while (nodes.MoveNext())
        {
            string type = nodes.Current.GetAttribute("type", "");
            if (uint.TryParse(nodes.Current.Value, out uint appId) && appId > 0)
            {
                appIds.Add(appId);
                types.Add(type);
            }
        }

        Assert.Equal(5, appIds.Count);
        Assert.Equal("normal", types[0]);
        Assert.Equal("demo", types[1]);
        Assert.Equal("", types[2]);
        Assert.Equal("junk", types[3]);
        Assert.Equal("mod", types[4]);
    }

    [Fact]
    public void XPathDocument_ParsesLargeXml_WithoutSkipping()
    {
        var appIds = new List<uint>();
        var xml = "<?xml version=\"1.0\"?><games>";
        for (int i = 0; i < 1000; i++)
            xml += $"<game>{i + 1}</game>";
        xml += "</games>";

        using var stringReader = new StringReader(xml);
        var document = new XPathDocument(stringReader);
        var navigator = document.CreateNavigator();
        var nodes = navigator.Select("/games/game");

        while (nodes.MoveNext())
        {
            if (uint.TryParse(nodes.Current.Value, out uint appId) && appId > 0)
                appIds.Add(appId);
        }

        Assert.Equal(1000, appIds.Count);
        Assert.Equal(appIds.Count, appIds.Distinct().Count());
    }
}
