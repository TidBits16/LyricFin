using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.LyricFin;

public static partial class Titles
{
    public static readonly IReadOnlyList<string> DefaultIgnoreTitleMarkers = ["🅴", "[Explicit]"];

    [GeneratedRegex(@"[\[\(]\s*Instrumental(?:\s+Version)?\s*[\]\)]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex InstrumentalMark();

    /// <summary>True when the title is marked instrumental, e.g. <c>(Instrumental)</c>.</summary>
    public static bool IsInstrumental(string? title)
        => !string.IsNullOrWhiteSpace(title) && InstrumentalMark().IsMatch(title);

    /// <summary>Strip configured markers (same behavior as MusicFin) before lyric lookup.</summary>
    public static string CleanForSearch(string value, IReadOnlyList<string>? markers = null)
        => StripMark(value ?? string.Empty, markers);

    public static string StripMark(string name, IReadOnlyList<string>? markers = null)
    {
        var s = name.Trim();
        foreach (var token in markers ?? DefaultIgnoreTitleMarkers)
        {
            s = StripToken(s, token);
        }

        return s.Trim();
    }

    private static string StripToken(string name, string token)
    {
        var mark = token.Trim();
        if (mark.Length == 0)
        {
            return name;
        }

        var s = name;
        foreach (var edge in new[] { mark, mark + " ", " " + mark })
        {
            if (s.StartsWith(edge, StringComparison.Ordinal))
            {
                s = s[edge.Length..].TrimStart();
                break;
            }
        }

        foreach (var edge in new[] { mark, " " + mark, mark + " " })
        {
            if (s.EndsWith(edge, StringComparison.Ordinal))
            {
                s = s[..^edge.Length].TrimEnd();
                break;
            }
        }

        return s;
    }
}
