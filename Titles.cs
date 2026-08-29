using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.LyricFin;

public static partial class Titles
{
    [GeneratedRegex(@"\s*[\[\(]?🅴[\]\)]?\s*", RegexOptions.Compiled)]
    private static partial Regex ExplicitEmoji();

    [GeneratedRegex(@"\s*[\[\(]\s*(?:E|Explicit)\s*[\]\)]\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ExplicitBracket();

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
