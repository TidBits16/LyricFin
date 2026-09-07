using System.Text;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.LyricFin.Configuration;

namespace Jellyfin.Plugin.LyricFin;

/// <summary>Masks swear words in LRC payloads while leaving timestamps and tags intact.</summary>
public static partial class LyricCensor
{
    /// <summary>Exact whole words (First-Letter/Full) and stems (Root).</summary>
    public static readonly IReadOnlyList<string> DefaultBlacklist =
    [
        "ass",
        "arse",
        "arsehole",
        "arseholes",
        "asshole",
        "assholes",
        "bastard",
        "bastards",
        "bitch",
        "bitches",
        "bitchy",
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

    /// <summary>Exact whole words that should never be censored.</summary>
    public static readonly IReadOnlyList<string> DefaultWhitelist =
    [
        "assassin",
        "assemble",
        "assembly",
        "assess",
        "assessment",
        "asset",
        "assets",
        "assign",
        "assist",
        "associate",
        "association",
        "assume",
        "assumed",
        "assumes",
        "assuming",
        "assurance",
        "bass",
        "class",
        "classic",
        "classical",
        "cocked",
        "cocktail",
        "cocktails",
        "compass",
        "dickens",
        "glass",
        "glasses",
        "hitchcock",
        "massachusetts",
        "pass",
        "passage",
        "passenger",
        "peacock",
        "shiitake",
    ];

    public static string DefaultWordListText
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Blacklist");
            foreach (var w in DefaultBlacklist)
            {
                sb.AppendLine(w);
            }

            sb.AppendLine();
            sb.AppendLine("# Whitelist");
            foreach (var w in DefaultWhitelist)
            {
                sb.AppendLine(w);
            }

            return sb.ToString().TrimEnd() + "\n";
        }
    }

    private const string RandomCharset = "&!$@#%?*";

    [GeneratedRegex(@"^(?:\[[^\]]*\])+", RegexOptions.CultureInvariant)]
    private static partial Regex LrcTagPrefix();

    [GeneratedRegex(@"[\p{L}\p{N}']+", RegexOptions.CultureInvariant)]
    private static partial Regex LyricToken();

    public readonly record struct CensorLists(
        IReadOnlyList<string> Blacklist,
        IReadOnlySet<string> Whitelist);

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
        if (lists.Blacklist.Count == 0)
        {
            return lrc;
        }

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

    public static CensorLists ParseLists(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new CensorLists(DefaultBlacklist, ToSet(DefaultWhitelist));
        }

        var blacklist = new List<string>();
        var whitelist = new List<string>();
        var section = "blacklist";
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
                if (IsBlacklistHeader(header))
                {
                    section = "blacklist";
                    sawHeader = true;
                    continue;
                }

                if (IsWhitelistHeader(header))
                {
                    section = "whitelist";
                    sawHeader = true;
                    continue;
                }

                // Legacy section names fold into blacklist.
                if (header.Equals("roots", StringComparison.OrdinalIgnoreCase)
                    || header.Equals("root", StringComparison.OrdinalIgnoreCase)
                    || header.Equals("stems", StringComparison.OrdinalIgnoreCase)
                    || header.Equals("words", StringComparison.OrdinalIgnoreCase)
                    || header.Equals("word", StringComparison.OrdinalIgnoreCase)
                    || header.Equals("exact", StringComparison.OrdinalIgnoreCase))
                {
                    section = "blacklist";
                    sawHeader = true;
                    continue;
                }

                continue;
            }

            foreach (var part in line.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.Length == 0)
                {
                    continue;
                }

                if (section == "whitelist")
                {
                    whitelist.Add(part);
                }
                else
                {
                    blacklist.Add(part);
                }
            }
        }

        if (!sawHeader)
        {
            return new CensorLists(
                DistinctList(blacklist.Count > 0 ? blacklist : DefaultBlacklist),
                ToSet(DefaultWhitelist));
        }

        return new CensorLists(
            DistinctList(blacklist.Count > 0 ? blacklist : DefaultBlacklist),
            ToSet(whitelist.Count > 0 ? whitelist : DefaultWhitelist));
    }

    private static bool IsBlacklistHeader(string header)
        => header.Equals("blacklist", StringComparison.OrdinalIgnoreCase)
           || header.Equals("black", StringComparison.OrdinalIgnoreCase)
           || header.Equals("block", StringComparison.OrdinalIgnoreCase)
           || header.Equals("blocklist", StringComparison.OrdinalIgnoreCase);

    private static bool IsWhitelistHeader(string header)
        => header.Equals("whitelist", StringComparison.OrdinalIgnoreCase)
           || header.Equals("white", StringComparison.OrdinalIgnoreCase)
           || header.Equals("allow", StringComparison.OrdinalIgnoreCase)
           || header.Equals("allowed", StringComparison.OrdinalIgnoreCase)
           || header.Equals("safe", StringComparison.OrdinalIgnoreCase);

    private static string CensorLine(
        string line,
        CensorLists lists,
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
            return tags + CensorRootsInText(text, lists.Blacklist, lists.Whitelist, style);
        }

        var alternation = string.Join(
            '|',
            lists.Blacklist.OrderByDescending(w => w.Length).Select(Regex.Escape));
        var wordRegex = new Regex(
            $@"(?<![\p{{L}}\p{{N}}'])({alternation})(?![\p{{L}}\p{{N}}'])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return tags + wordRegex.Replace(text, m =>
        {
            if (lists.Whitelist.Contains(m.Value))
            {
                return m.Value;
            }

            return MaskWhole(m.Value, mode, style);
        });
    }

    private static string CensorRootsInText(
        string text,
        IReadOnlyList<string> stems,
        IReadOnlySet<string> whitelist,
        CensorSymbolStyle style)
    {
        var ordered = stems
            .Where(r => r.Length > 0)
            .OrderByDescending(r => r.Length)
            .ToArray();
        if (ordered.Length == 0)
        {
            return text;
        }

        return LyricToken().Replace(text, m => MaskTokenWithRoot(m.Value, ordered, whitelist, style));
    }

    private static string MaskTokenWithRoot(
        string token,
        IReadOnlyList<string> stemsLongestFirst,
        IReadOnlySet<string> whitelist,
        CensorSymbolStyle style)
    {
        if (whitelist.Contains(token))
        {
            return token;
        }

        var lower = token.ToLowerInvariant();
        foreach (var stem in stemsLongestFirst)
        {
            var idx = lower.IndexOf(stem, StringComparison.Ordinal);
            if (idx < 0)
            {
                continue;
            }

            // Short stems ("ass") only at the start — avoids glass/bass/pass.
            // Longer stems ("fuck") may sit inside compounds (motherf***ing).
            if (idx > 0 && stem.Length < 4)
            {
                continue;
            }

            var matched = token.Substring(idx, stem.Length);
            var masked = MaskWhole(matched, CensorMode.Ending, style);
            return token[..idx] + masked + token[(idx + stem.Length)..];
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

    private static HashSet<string> ToSet(IEnumerable<string> source)
        => new(DistinctList(source), StringComparer.OrdinalIgnoreCase);

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
