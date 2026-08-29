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
}
