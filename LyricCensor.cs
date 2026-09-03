using System.Text;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.LyricFin.Configuration;

namespace Jellyfin.Plugin.LyricFin;

/// <summary>Masks swear words in LRC payloads while leaving timestamps and tags intact.</summary>
public static partial class LyricCensor
{
    public static readonly IReadOnlyList<string> DefaultWords =
    [
        "asshole",
        "assholes",
        "arsehole",
        "arseholes",
        "bastard",
        "bastards",
        "bitches",
        "bitchy",
        "bitching",
        "bullshit",
        "cocksucker",
        "cocksuckers",
        "dickhead",
        "dickheads",
        "fucked",
        "fucker",
        "fuckers",
        "fuckin",
        "fucking",
        "fucks",
        "motherfucker",
        "motherfuckers",
        "motherfucking",
        "niggas",
        "niggers",
        "pussies",
        "shits",
        "shitting",
        "shitty",
        "twats",
        "wankers",
    ];

    public static readonly IReadOnlyList<string> DefaultRoots =
    [
        "ass",
        "arse",
        "bastard",
        "bitch",
        "cock",
        "cunt",
        "dick",
        "fuck",
        "nigger",
        "nigga",
        "pussy",
        "shit",
        "twat",
        "wanker",
    ];

    /// <summary>Built-in list with # Words / # Roots sections for the settings editor.</summary>
    public static string DefaultWordListText
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Words");
            foreach (var w in DefaultWords)
            {
                sb.AppendLine(w);
            }

            sb.AppendLine();
            sb.AppendLine("# Roots");
            foreach (var w in DefaultRoots)
            {
                sb.AppendLine(w);
            }

            return sb.ToString().TrimEnd() + "\n";
        }
    }

    /// <summary>Flat fallback when a list has no section headers (legacy).</summary>
    public static readonly IReadOnlyList<string> DefaultWordList =
        DefaultWords.Concat(DefaultRoots).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private const string RandomCharset = "&!$@#%?*";

    [GeneratedRegex(@"^(?:\[[^\]]*\])+", RegexOptions.CultureInvariant)]
    private static partial Regex LrcTagPrefix();

    [GeneratedRegex(@"[\p{L}\p{N}']+", RegexOptions.CultureInvariant)]
    private static partial Regex LyricToken();

    public static string Apply(
        string lrc,
        CensorMode mode,
        CensorSymbolStyle style,
        string? wordListText = null)
    {
        if (mode == CensorMode.None || string.IsNullOrEmpty(lrc))
        {
            return lrc;
        }

        var lists = ParseLists(wordListText);
        var sb = new StringBuilder(lrc.Length);
        var first = true;
        foreach (var line in SplitLines(lrc))
        {
            if (!first)
            {
                sb.Append('\n');
            }

            first = false;
            sb.Append(CensorLine(line, lists, mode, style));
        }

        return sb.ToString();
    }

    public static (IReadOnlyList<string> Words, IReadOnlyList<string> Roots) ParseLists(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (DefaultWords, DefaultRoots);
        }

        var words = new List<string>();
        var roots = new List<string>();
        var section = "words";
        var sawHeader = false;

        foreach (var rawLine in SplitLines(raw))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith('#'))
            {
                var header = line.TrimStart('#').Trim();
                if (header.Equals("words", StringComparison.OrdinalIgnoreCase)
                    || header.Equals("word", StringComparison.OrdinalIgnoreCase)
                    || header.Equals("exact", StringComparison.OrdinalIgnoreCase))
                {
                    section = "words";
                    sawHeader = true;
                    continue;
                }

                if (header.Equals("roots", StringComparison.OrdinalIgnoreCase)
                    || header.Equals("root", StringComparison.OrdinalIgnoreCase)
                    || header.Equals("stems", StringComparison.OrdinalIgnoreCase))
                {
                    section = "roots";
                    sawHeader = true;
                    continue;
                }

                // Other # comments are ignored.
                continue;
            }

            foreach (var part in line.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.Length == 0)
                {
                    continue;
                }

                if (section == "roots")
                {
                    roots.Add(part);
                }
                else
                {
                    words.Add(part);
                }
            }
        }

        if (!sawHeader)
        {
            // Legacy flat list: treat everything as words; keep built-in roots for Root mode.
            return (
                DistinctList(words.Count > 0 ? words : DefaultWords),
                DefaultRoots);
        }

        return (
            DistinctList(words.Count > 0 ? words : DefaultWords),
            DistinctList(roots.Count > 0 ? roots : DefaultRoots));
    }

    private static string CensorLine(
        string line,
        (IReadOnlyList<string> Words, IReadOnlyList<string> Roots) lists,
        CensorMode mode,
        CensorSymbolStyle style)
    {
        var prefix = LrcTagPrefix().Match(line);
        var tags = prefix.Success ? prefix.Value : string.Empty;
        var text = prefix.Success ? line[prefix.Length..] : line;
        if (text.Length == 0)
        {
            return line;
        }

        if (mode == CensorMode.Root)
        {
            return tags + CensorRootsInText(text, lists.Roots, style);
        }

        // Ending / Full: whole-word match against Words + Roots.
        var whole = DistinctList(lists.Words.Concat(lists.Roots));
        if (whole.Count == 0)
        {
            return line;
        }

        var alternation = string.Join(
            '|',
            whole.OrderByDescending(w => w.Length).Select(Regex.Escape));
        var wordRegex = new Regex(
            $@"(?<![\p{{L}}\p{{N}}'])({alternation})(?![\p{{L}}\p{{N}}'])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return tags + wordRegex.Replace(text, m => MaskWhole(m.Value, mode, style));
    }

    private static string CensorRootsInText(
        string text,
        IReadOnlyList<string> roots,
        CensorSymbolStyle style)
    {
        if (roots.Count == 0)
        {
            return text;
        }

        var ordered = roots
            .Where(r => r.Length > 0)
            .OrderByDescending(r => r.Length)
            .ToArray();
        if (ordered.Length == 0)
        {
            return text;
        }

        return LyricToken().Replace(text, m => MaskTokenWithRoot(m.Value, ordered, style));
    }

    private static string MaskTokenWithRoot(
        string token,
        IReadOnlyList<string> rootsLongestFirst,
        CensorSymbolStyle style)
    {
        var lower = token.ToLowerInvariant();
        foreach (var root in rootsLongestFirst)
        {
            var idx = lower.IndexOf(root, StringComparison.Ordinal);
            if (idx < 0)
            {
                continue;
            }

            var matched = token.Substring(idx, root.Length);
            var masked = MaskWhole(matched, CensorMode.Ending, style);
            return token[..idx] + masked + token[(idx + root.Length)..];
        }

        return token;
    }

    private static string MaskWhole(string word, CensorMode mode, CensorSymbolStyle style)
    {
        if (word.Length == 0)
        {
            return word;
        }

        if (mode == CensorMode.Ending && word.Length == 1)
        {
            return word;
        }

        // Root mode reuses Ending masking for the matched root span.
        var start = mode is CensorMode.Ending or CensorMode.Root ? 1 : 0;
        var chars = new char[word.Length];
        char? previous = null;
        for (var i = 0; i < word.Length; i++)
        {
            if (i < start)
            {
                chars[i] = word[i];
                previous = null;
                continue;
            }

            chars[i] = SymbolAt(word, i, style, previous);
            previous = chars[i];
        }

        return new string(chars);
    }

    private static char SymbolAt(string word, int index, CensorSymbolStyle style, char? previous)
    {
        if (style == CensorSymbolStyle.Dashes)
        {
            return '-';
        }

        if (style != CensorSymbolStyle.Random)
        {
            return '*';
        }

        Span<char> pool = stackalloc char[RandomCharset.Length];
        var n = 0;
        foreach (var c in RandomCharset)
        {
            if (previous is char p && c == p)
            {
                continue;
            }

            pool[n++] = c;
        }

        if (n == 0)
        {
            return RandomCharset[0];
        }

        return pool[Math.Abs(HashCode.Combine(word.ToLowerInvariant(), index)) % n];
    }

    private static List<string> DistinctList(IEnumerable<string> source)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();
        foreach (var w in source)
        {
            var t = w.Trim();
            if (t.Length > 0 && set.Add(t))
            {
                list.Add(t);
            }
        }

        return list;
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            yield return line;
        }
    }
}
