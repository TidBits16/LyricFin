using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.LyricFin.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Concurrent track workers. Keep low — LRCLIB asks for polite sequential requests. 0 = 1.</summary>
    public int Workers { get; set; } = 1;
}
