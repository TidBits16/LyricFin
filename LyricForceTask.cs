using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.LyricFin;

/// <summary>Manual-only force refetch (queued from settings so the HTTP request does not time out).</summary>
public class LyricForceTask : IScheduledTask
{
    private readonly LyricEngine _engine;
    private readonly ILogger<LyricForceTask> _logger;

    public LyricForceTask(LyricEngine engine, ILogger<LyricForceTask> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public string Name => "LyricFin: Fetch All Lyrics";

    public string Key => "LyricFinForceAll";

    public string Description =>
        "Force-fetch timed LRC for every track (overwrites). Clears lyrics on (Instrumental) titles when that option is enabled.";

    public string Category => "Library";

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        try
        {
            await _engine.RunAsync(force: true, progress, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LyricFin force fetch failed");
            throw;
        }
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];
}
