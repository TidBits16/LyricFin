using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.LyricFin;

public static partial class Titles
{
    [GeneratedRegex(@"\s*[\[\(]?🅴[\]\)]?\s*", RegexOptions.Compiled)]
    private static partial Regex ExplicitEmoji();

    [GeneratedRegex(@"\s*[\[\(]\s*(?:E|Explicit)\s*[\]\)]\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ExplicitBracket();

    [GeneratedRegex(@"[\[\(]\s*Instrumental(?:\s+Version)?\s*[\]\)]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex InstrumentalMark();

    /// <summary>True when the title is marked instrumental, e.g. <c>(Instrumental)</c>.</summary>
    public static bool IsInstrumental(string? title)
        => !string.IsNullOrWhiteSpace(title) && InstrumentalMark().IsMatch(title);

    /// <summary>Strip ExplicitFin-style marks so lyric lookups still match.</summary>
    public static string CleanForSearch(string value)
    {
        var s = (value ?? string.Empty).Trim();
        if (s.Length == 0)
        {
            return s;
        }

        s = ExplicitEmoji().Replace(s, " ");
        s = ExplicitBracket().Replace(s, " ");
        while (s.Contains("  ", StringComparison.Ordinal))
        {
            s = s.Replace("  ", " ", StringComparison.Ordinal);
        }

        return s.Trim();
    }
}
