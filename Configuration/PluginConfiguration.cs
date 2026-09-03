using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.LyricFin.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Concurrent track workers. Keep low - LRCLIB asks for polite sequential requests. 0 = 1.</summary>
    public int Workers { get; set; } = 1;

    /// <summary>
    /// Skip titles marked (Instrumental) / [Instrumental]. Scheduled runs leave them alone;
    /// force fetch clears any existing lyrics on those tracks.
    /// </summary>
    public bool SkipInstrumentals { get; set; } = true;

    /// <summary>Comma-separated suffix/prefix markers stripped from titles before lookup (same as MusicFin).</summary>
    public string IgnoreTitleMarkers { get; set; } = "🅴,[Explicit]";

    /// <summary>How swear words are masked: None, Ending, Full.</summary>
    public string CensorMode { get; set; } = "None";

    /// <summary>Mask character style: Asterisks, Dashes, Random.</summary>
    public string CensorSymbolStyle { get; set; } = "Asterisks";

    /// <summary>One word per line (commas also fine). Empty falls back to the built-in list.</summary>
    public string CensorWords { get; set; } = LyricCensor.DefaultWordListText;

    public IReadOnlyList<string> EffectiveIgnoreTitleMarkers
        => ParseList(IgnoreTitleMarkers, Titles.DefaultIgnoreTitleMarkers);

    public IReadOnlyList<string> EffectiveCensorWords
        => ParseList(CensorWords, LyricCensor.DefaultWordList);

    public CensorMode EffectiveCensorMode
        => (CensorMode ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "ending" => Configuration.CensorMode.Ending,
            "full" => Configuration.CensorMode.Full,
            _ => Configuration.CensorMode.None,
        };

    public CensorSymbolStyle EffectiveCensorSymbolStyle
        => (CensorSymbolStyle ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "dashes" => Configuration.CensorSymbolStyle.Dashes,
            "random" => Configuration.CensorSymbolStyle.Random,
            _ => Configuration.CensorSymbolStyle.Asterisks,
        };

    private static IReadOnlyList<string> ParseList(string raw, IReadOnlyList<string> fallback)
    {
        var items = (raw ?? string.Empty)
            .Split([',', ';', '\n', '\r'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return items.Count > 0 ? items : fallback;
    }
}
