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

    public IReadOnlyList<string> EffectiveIgnoreTitleMarkers
        => ParseList(IgnoreTitleMarkers, Titles.DefaultIgnoreTitleMarkers);

    private static IReadOnlyList<string> ParseList(string raw, IReadOnlyList<string> fallback)
    {
        var items = (raw ?? string.Empty)
            .Split([',', ';', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        return items.Count > 0 ? items : fallback;
    }
}
