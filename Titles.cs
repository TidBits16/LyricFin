using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.LyricTagShelf;

public static partial class Titles
{
    public static readonly IReadOnlyList<string> DefaultIgnoreTitleMarkers = ["🅴", "[Explicit]"];

    [GeneratedRegex(@"[\[\(]\s*Instrumental(?:\s+Version)?\s*[\]\)]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex InstrumentalMark();

    /// <summary>True when the title is marked instrumental, e.g. <c>(Instrumental)</c>.</summary>
    public static bool IsInstrumental(string? title)
        => !string.IsNullOrWhiteSpace(title) && InstrumentalMark().IsMatch(title);

    /// <summary>
    /// Strips a trailing " - Artist" when the suffix matches <paramref name="artist"/>.
    /// </summary>
    public static string StripTrailingArtist(string title, string artist)
    {
        var t = title.Trim();
        var a = artist.Trim();
        if (t.Length == 0 || a.Length == 0)
        {
            return t;
        }

        foreach (var sep in new[] { " - ", " – ", " -- " })
        {
            var idx = t.LastIndexOf(sep, StringComparison.Ordinal);
            if (idx <= 0)
            {
                continue;
            }

            var suffix = t[(idx + sep.Length)..].Trim();
            if (suffix.Equals(a, StringComparison.OrdinalIgnoreCase))
            {
                return t[..idx].TrimEnd();
            }
        }

        return t;
    }

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
        if (s.StartsWith(mark, StringComparison.Ordinal))
        {
            s = s[mark.Length..];
        }

        if (s.EndsWith(mark, StringComparison.Ordinal))
        {
            s = s[..^mark.Length];
        }

        return s.Trim();
    }
}
