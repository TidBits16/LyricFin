using System.Text;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.LyricFin.Configuration;

namespace Jellyfin.Plugin.LyricFin;

/// <summary>Masks swear words in LRC payloads while leaving timestamps and tags intact.</summary>
public static partial class LyricCensor
{
    public static readonly IReadOnlyList<string> DefaultWordList =
    [
        "asshole",
        "assholes",
        "bastard",
        "bastards",
        "bitch",
        "bitches",
        "bitching",
        "bullshit",
        "cock",
        "cocks",
        "cocksucker",
        "cocksuckers",
        "cunt",
        "cunts",
        "dick",
        "dickhead",
        "dickheads",
        "dicks",
        "fuck",
        "fucked",
        "fucker",
        "fuckers",
        "fuckin",
        "fucking",
        "fucks",
        "motherfucker",
        "motherfuckers",
        "motherfucking",
        "nigga",
        "niggas",
        "nigger",
        "niggers",
        "pussies",
        "pussy",
        "shit",
        "shits",
        "shitting",
        "shitty",
        "twat",
        "twats",
        "wanker",
        "wankers",
    ];

    public static string DefaultWordListText => string.Join('\n', DefaultWordList);

    private const string RandomCharset = "&!$@#%?*";

    [GeneratedRegex(@"^(?:\[[^\]]*\])+", RegexOptions.CultureInvariant)]
    private static partial Regex LrcTagPrefix();

    public static string Apply(
        string lrc,
        CensorMode mode,
        CensorSymbolStyle style,
        IReadOnlyList<string>? words = null)
    {
        if (mode == CensorMode.None || string.IsNullOrEmpty(lrc))
        {
            return lrc;
        }

        var list = NormalizeWords(words);
        if (list.Count == 0)
        {
            return lrc;
        }

        // Longer first so "motherfucker" wins over "fuck".
        var alternation = string.Join(
            '|',
            list.OrderByDescending(w => w.Length).Select(Regex.Escape));
        var wordRegex = new Regex(
            $@"(?<![\p{{L}}\p{{N}}'])({alternation})(?![\p{{L}}\p{{N}}'])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        var sb = new StringBuilder(lrc.Length);
        var first = true;
        foreach (var line in SplitLines(lrc))
        {
            if (!first)
            {
                sb.Append('\n');
            }

            first = false;
            sb.Append(CensorLine(line, wordRegex, mode, style));
        }

        return sb.ToString();
    }

    private static string CensorLine(
        string line,
        Regex wordRegex,
        CensorMode mode,
        CensorSymbolStyle style)
    {
        var prefix = LrcTagPrefix().Match(line);
        if (prefix.Success)
        {
            var tags = prefix.Value;
            var text = line[prefix.Length..];
            if (text.Length == 0)
            {
                return line;
            }

            return tags + wordRegex.Replace(text, m => Mask(m.Value, mode, style));
        }

        return wordRegex.Replace(line, m => Mask(m.Value, mode, style));
    }

    private static string Mask(string word, CensorMode mode, CensorSymbolStyle style)
    {
        if (word.Length == 0)
        {
            return word;
        }

        if (mode == CensorMode.Ending && word.Length == 1)
        {
            return word;
        }

        var start = mode == CensorMode.Ending ? 1 : 0;
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

    private static List<string> NormalizeWords(IReadOnlyList<string>? words)
    {
        var source = words is { Count: > 0 } ? words : DefaultWordList;
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in source)
        {
            var t = w.Trim();
            if (t.Length > 0)
            {
                set.Add(t);
            }
        }

        return set.ToList();
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
