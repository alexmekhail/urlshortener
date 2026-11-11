using UrlShortener.Models;
using UrlShortener.Services;
using Xunit;

namespace UrlShortener.Tests;

public class UserAgentParserTests
{
    [Fact]
    public void Parse_NullOrEmpty_ReturnsUnknown()
    {
        var (device, browser) = UserAgentParser.Parse(null);
        Assert.Equal(DeviceTypes.Unknown, device);
        Assert.Null(browser);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36", DeviceTypes.Desktop, "Chrome")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/605.1.15", DeviceTypes.Desktop, "Safari")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0", DeviceTypes.Desktop, "Firefox")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 Edg/129.0.0.0", DeviceTypes.Desktop, "Edge")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1", DeviceTypes.Mobile, "Safari")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Mobile Safari/537.36", DeviceTypes.Mobile, "Chrome")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/604.1", DeviceTypes.Tablet, "Safari")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; SM-X200) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36", DeviceTypes.Tablet, "Chrome")]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)", DeviceTypes.Bot, null)]
    [InlineData("facebookexternalhit/1.1", DeviceTypes.Bot, null)]
    public void Parse_ClassifiesDeviceAndBrowser(string userAgent, string expectedDevice, string? expectedBrowser)
    {
        var (device, browser) = UserAgentParser.Parse(userAgent);
        Assert.Equal(expectedDevice, device);
        Assert.Equal(expectedBrowser, browser);
    }
}
