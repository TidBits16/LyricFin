using Jellyfin.Data.Enums;
using Jellyfin.Plugin.LyricFin.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Lyrics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.LyricFin;

public class LyricEngine
{
    private readonly ILibraryManager _library;
    private readonly ILyricManager _lyrics;
    private readonly LrcLibClient _lrclib;
    private readonly ILogger<LyricEngine> _logger;

    public LyricEngine(
        ILibraryManager library,
        ILyricManager lyrics,
        LrcLibClient lrclib,
        ILogger<LyricEngine> logger)
    {
        _library = library;
        _lyrics = lyrics;
        _lrclib = lrclib;
        _logger = logger;
    }

    /// <param name="force">When true, refetch and overwrite even if lyrics already exist.</param>
    public async Task<LyricRunResult> RunAsync(
        bool force,
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var workers = cfg.Workers <= 0 ? 1 : cfg.Workers;
        workers = Math.Clamp(workers, 1, 4);

        var tracks = _library.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            Recursive = true
        }).OfType<Audio>().Where(t => t.Id != Guid.Empty).ToList();

        var targets = force
            ? tracks
            : tracks.Where(t => t.HasLyrics != true).ToList();

        _logger.LogInformation(
            "LyricFin: {Targets}/{Total} tracks ({Mode}), {Workers} workers",
            targets.Count,
            tracks.Count,
            force ? "force all" : "missing only",
            workers);

        var saved = 0;
        var skipped = tracks.Count - targets.Count;
        var failed = 0;
        var done = 0;
        var total = Math.Max(1, targets.Count);

        using var gate = new SemaphoreSlim(workers, workers);
        await Task.WhenAll(targets.Select(async track =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ok = await ProcessTrackAsync(track, cancellationToken).ConfigureAwait(false);
                if (ok)
                {
                    Interlocked.Increment(ref saved);
                }
                else
                {
                    Interlocked.Increment(ref failed);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failed);
                _logger.LogWarning(ex, "LyricFin failed on {Id} ({Name})", track.Id, track.Name);
            }
            finally
            {
                var n = Interlocked.Increment(ref done);
                progress.Report(100.0 * n / total);
                gate.Release();
            }
        })).ConfigureAwait(false);

        progress.Report(100);
        _logger.LogInformation(
            "LyricFin finished: saved {Saved}, no timed lyrics {Missed}, already had {Skipped}, http {Http}/{Cache} cache",
            saved,
            failed,
            skipped,
            _lrclib.HttpCount,
            _lrclib.CacheHits);

        return new LyricRunResult(saved, failed, skipped);
    }

    private async Task<bool> ProcessTrackAsync(Audio track, CancellationToken cancellationToken)
    {
        var title = Titles.CleanForSearch(track.Name ?? string.Empty);
        var artist = PrimaryArtist(track);
        if (title.Length == 0 || artist.Length == 0)
        {
            return false;
        }

        var album = Titles.CleanForSearch(track.Album ?? string.Empty);
        double? duration = null;
        if (track.RunTimeTicks is > 0)
        {
            duration = TimeSpan.FromTicks(track.RunTimeTicks.Value).TotalSeconds;
        }

        var hit = await _lrclib.FindSyncedAsync(title, artist, album, duration, cancellationToken)
            .ConfigureAwait(false);
        if (hit is null)
        {
            return false;
        }

        var saved = await _lyrics.SaveLyricAsync(track, "lrc", hit.SyncedLyrics).ConfigureAwait(false);
        if (saved is null)
        {
            _logger.LogWarning(
                "LyricFin: Jellyfin rejected LRC for {Id} ({Name}) from {Source}",
                track.Id,
                track.Name,
                hit.Source);
            return false;
        }

        _logger.LogInformation(
            "LyricFin saved timed lyrics for {Id}: {Name} ({Source})",
            track.Id,
            track.Name,
            hit.Source);
        return true;
    }

    private static string PrimaryArtist(Audio track)
    {
        var artists = track.Artists;
        if (artists is { Count: > 0 } && !string.IsNullOrWhiteSpace(artists[0]))
        {
            return Titles.CleanForSearch(artists[0]);
        }

        var albumArtists = track.AlbumArtists;
        if (albumArtists is { Count: > 0 } && !string.IsNullOrWhiteSpace(albumArtists[0]))
        {
            return Titles.CleanForSearch(albumArtists[0]);
        }

        return string.Empty;
    }
}

public readonly record struct LyricRunResult(int Saved, int Missed, int Skipped);
