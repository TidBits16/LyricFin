using System.Text;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.LyricFin.Configuration;

namespace Jellyfin.Plugin.LyricFin;

/// <summary>
/// Masks swear words in LRC payloads while leaving timestamps and tags intact.
/// Matching is whole-word only: tokens are split on spaces, hyphens, and apostrophes, so
/// <c>ass</c> does not match <c>glass</c>; compounds like <c>asshole</c> must be listed.
/// Apostrophes separate too (<c>shit's</c> → <c>shit</c>, <c>fuckin'</c> → <c>fuckin</c>).
/// </summary>
public static partial class LyricCensor
{
    /// <summary>Default whole words to censor. Add compounds explicitly (asshole, motherfucker, …).</summary>
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
        "bitchin",
        "bitchy",
        "bitching",
        "bullshit",
        "cock",
        "cocks",
        "cocksucker",
        "cocksuckers",
        "cunt",
        "cunts",
        "damn",
        "dammit",
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
        "motherfuckin",
        "motherfucking",
        "nigga",
        "niggas",
        "nigger",
        "niggers",
        "pussies",
        "pussy",
        "shit",
        "shits",
        "shittin",
        "shitting",
        "shitty",
        "twat",
        "twats",
        "wanker",
        "wankers",
    ];

    public static string DefaultWordListText
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Blacklist");
            sb.AppendLine("# One word per line. Matching is whole-word only (split on spaces, hyphens, and apostrophes).");
            sb.AppendLine("# So shit's → shit + ' + s, and fuckin' → fuckin.");
            sb.AppendLine("# Add compounds yourself (e.g. asshole) — short words like ass will not match inside glass.");
            foreach (var w in DefaultBlacklist)
            {
                sb.AppendLine(w);
            }

            return sb.ToString().TrimEnd() + "\n";
        }
    }

    private const string RandomCharset = "&!$@#%?*";

    [GeneratedRegex(@"^(?:\[[^\]]*\])+", RegexOptions.CultureInvariant)]
    private static partial Regex LrcTagPrefix();

    /// <summary>Letter/number runs (words) or everything else (spaces, hyphens, apostrophes, punctuation).</summary>
    [GeneratedRegex(@"[\p{L}\p{N}]+|[^\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex LyricChunks();

    public readonly record struct CensorLists(IReadOnlySet<string> Blacklist);

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

        // Legacy Root mode behaves like First-Letter (whole-word mask, keep first letter).
        if (mode == CensorMode.Root)
        {
            mode = CensorMode.Ending;
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
            return new CensorLists(ToSet(DefaultBlacklist));
        }

        var blacklist = new List<string>();
        var inWhitelist = false;
        var sawAnyWord = false;

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
                // Ignore whitelist / legacy section headers; only blacklist words are used.
                inWhitelist = IsWhitelistHeader(header);
                continue;
            }

            if (inWhitelist)
            {
                continue;
            }

            foreach (var part in line.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.Length == 0)
                {
                    continue;
                }

                sawAnyWord = true;
                blacklist.Add(part);
            }
        }

        return new CensorLists(ToSet(sawAnyWord ? blacklist : DefaultBlacklist));
    }

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

        var sb = new StringBuilder(text.Length);
        foreach (Match chunk in LyricChunks().Matches(text))
        {
            var value = chunk.Value;
            // Hyphen and space (and other non-word runs) are separators — never censor them.
            if (!IsWordToken(value))
            {
                sb.Append(value);
                continue;
            }

            if (IsBlacklisted(value, lists.Blacklist))
            {
                sb.Append(MaskWhole(value, mode, style));
            }
            else
            {
                sb.Append(value);
            }
        }

        return tags + sb;
    }

    /// <summary>
    /// Apostrophes are word separators (shit's → shit). Also try the -ing form when the
    /// token ends in -in so older lists with "fucking" still catch "fuckin".
    /// </summary>
    private static bool IsBlacklisted(string token, IReadOnlySet<string> blacklist)
    {
        if (blacklist.Contains(token))
        {
            return true;
        }

        // fuckin / motherfuckin / bitchin → fucking / motherfucking / bitching
        if (token.EndsWith("in", StringComparison.OrdinalIgnoreCase)
            && !token.EndsWith("ing", StringComparison.OrdinalIgnoreCase)
            && blacklist.Contains(token + "g"))
        {
            return true;
        }

        return false;
    }

    private static bool IsWordToken(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        // Word tokens are letter/digit runs from LyricChunks; hyphens/apostrophes are separate.
        var c = value[0];
        return char.IsLetter(c) || char.IsDigit(c);
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
