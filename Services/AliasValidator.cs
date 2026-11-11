using System.Text.RegularExpressions;

namespace UrlShortener.Services;

public static partial class AliasValidator
{
    public const int MinLength = 3;
    public const int MaxLength = 30;

    public static bool IsValidFormat(string alias) =>
        alias.Length is >= MinLength and <= MaxLength && ValidCharsRegex().IsMatch(alias);

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex ValidCharsRegex();
}
