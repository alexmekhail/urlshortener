using UrlShortener.Models;

namespace UrlShortener.Services;

/// <summary>
/// Lightweight heuristic User-Agent parser — no external dependency. Good enough
/// to bucket clicks by device type and browser family for analytics; not meant
/// to be a precise UA database.
/// </summary>
public static class UserAgentParser
{
    private static readonly string[] BotTokens =
        ["bot", "crawler", "spider", "slurp", "bingpreview", "facebookexternalhit", "whatsapp", "telegrambot"];

    public static (string DeviceType, string? Browser) Parse(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return (DeviceTypes.Unknown, null);

        var ua = userAgent;

        if (ContainsAny(ua, BotTokens))
            return (DeviceTypes.Bot, ExtractBrowser(ua));

        return (ClassifyDevice(ua), ExtractBrowser(ua));
    }

    private static string ClassifyDevice(string ua)
    {
        var isAndroid = Has(ua, "Android");
        var isMobileToken = Has(ua, "Mobi") || Has(ua, "iPhone") || Has(ua, "iPod");

        // Android tablets omit "Mobile" from the UA string; Android phones include it.
        if (Has(ua, "iPad") || (isAndroid && !Has(ua, "Mobile")))
            return DeviceTypes.Tablet;

        if (isMobileToken || isAndroid)
            return DeviceTypes.Mobile;

        return DeviceTypes.Desktop;
    }

    private static string? ExtractBrowser(string ua)
    {
        // Order matters: several browsers' UA strings contain "Chrome" and/or
        // "Safari" as compatibility tokens, so the more specific checks go first.
        if (Has(ua, "Edg/")) return "Edge";
        if (Has(ua, "OPR/") || Has(ua, "Opera")) return "Opera";
        if (Has(ua, "CriOS")) return "Chrome";
        if (Has(ua, "FxiOS") || Has(ua, "Firefox")) return "Firefox";
        if (Has(ua, "Chrome") && !Has(ua, "Chromium")) return "Chrome";
        if (Has(ua, "Chromium")) return "Chromium";
        if (Has(ua, "Safari")) return "Safari";
        if (Has(ua, "MSIE") || Has(ua, "Trident")) return "Internet Explorer";
        return null;
    }

    private static bool ContainsAny(string haystack, IEnumerable<string> needles) =>
        needles.Any(n => Has(haystack, n));

    private static bool Has(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
