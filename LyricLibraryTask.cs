using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.LyricFin;

public class LyricLibraryTask : IScheduledTask
{
    private readonly LyricEngine _engine;
    private readonly ILogger<LyricLibraryTask> _logger;

    public LyricLibraryTask(LyricEngine engine, ILogger<LyricLibraryTask> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public string Name => "LyricFin: Get Timed Lyrics";

    public string Key => "LyricFinLibrary";

    public string Description =>
        "Fetches missing timed LRC lyrics from LRCLIB (synced only). Skips (Instrumental) titles when enabled.";

    public string Category => "Library";

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        try
        {
            await _engine.RunAsync(force: false, progress, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LyricFin failed");
            throw;
        }
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            }
        ];
    }
}
