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
    private int _forceNext;

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

    /// <summary>Next scheduled run overwrites existing lyrics (settings button).</summary>
    public void RequestForce() => Interlocked.Exchange(ref _forceNext, 1);

    public Task<LyricRunResult> RunAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var force = Interlocked.Exchange(ref _forceNext, 0) == 1;
        return RunAsync(force, progress, cancellationToken);
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
        var skipInstrumentals = cfg.SkipInstrumentals;

        var tracks = _library.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            Recursive = true
        }).OfType<Audio>().Where(t => t.Id != Guid.Empty).ToList();

        var instrumentals = skipInstrumentals
            ? tracks.Where(t => Titles.IsInstrumental(t.Name)).ToList()
            : [];
        var nonInstrumentals = skipInstrumentals
            ? tracks.Where(t => !Titles.IsInstrumental(t.Name)).ToList()
            : tracks;

        // Force: clear lyrics on instrumentals. Missing-only: leave them alone (count as skipped).
        var clearTargets = force ? instrumentals : [];
        var fetchTargets = force
            ? nonInstrumentals
            : nonInstrumentals.Where(t => t.HasLyrics != true).ToList();

        var skipped = tracks.Count - fetchTargets.Count - clearTargets.Count;

        _logger.LogInformation(
            "LyricFin: fetch {Fetch}/{Total}, clear instrumentals {Clear} ({Mode}), {Workers} workers, skipInstrumentals={Skip}",
            fetchTargets.Count,
            tracks.Count,
            clearTargets.Count,
            force ? "force all" : "missing only",
            workers,
            skipInstrumentals);

        var saved = 0;
        var failed = 0;
        var cleared = 0;
        var done = 0;
        var workItems = clearTargets.Count + fetchTargets.Count;
        var total = Math.Max(1, workItems);

        using var gate = new SemaphoreSlim(workers, workers);

        async Task WorkAsync(Audio track, bool clearOnly)
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (clearOnly)
                {
                    if (track.HasLyrics == true)
                    {
                        await _lyrics.DeleteLyricsAsync(track).ConfigureAwait(false);
                        Interlocked.Increment(ref cleared);
                        _logger.LogInformation(
                            "LyricFin cleared lyrics on instrumental {Id}: {Name}",
                            track.Id,
                            track.Name);
                    }
                    return;
                }

                var ok = await ProcessTrackAsync(track, cfg, cancellationToken)
                    .ConfigureAwait(false);
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
        }

        await Task.WhenAll(
            clearTargets.Select(t => WorkAsync(t, clearOnly: true))
                .Concat(fetchTargets.Select(t => WorkAsync(t, clearOnly: false))))
            .ConfigureAwait(false);

        progress.Report(100);
        _logger.LogInformation(
            "LyricFin finished: saved {Saved}, cleared {Cleared}, no timed lyrics {Missed}, skipped {Skipped}, http {Http}/{Cache} cache",
            saved,
            cleared,
            failed,
            skipped,
            _lrclib.HttpCount,
            _lrclib.CacheHits);

        return new LyricRunResult(saved, failed, skipped, cleared);
    }

    private async Task<bool> ProcessTrackAsync(
        Audio track,
        PluginConfiguration cfg,
        CancellationToken cancellationToken)
    {
        var markers = cfg.EffectiveIgnoreTitleMarkers;
        var title = Titles.CleanForSearch(track.Name ?? string.Empty, markers);
        var artist = PrimaryArtist(track);
        if (title.Length == 0 || artist.Length == 0)
        {
            return false;
        }

        var album = Titles.CleanForSearch(track.Album ?? string.Empty, markers);
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

        var lyrics = LyricCensor.Apply(
            hit.SyncedLyrics,
            cfg.EffectiveCensorMode,
            cfg.EffectiveCensorSymbolStyle,
            cfg.CensorWords);
        var saved = await _lyrics.SaveLyricAsync(track, "lrc", lyrics).ConfigureAwait(false);
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
            return artists[0].Trim();
        }

        var albumArtists = track.AlbumArtists;
        if (albumArtists is { Count: > 0 } && !string.IsNullOrWhiteSpace(albumArtists[0]))
        {
            return albumArtists[0].Trim();
        }

        return string.Empty;
    }
}

public readonly record struct LyricRunResult(int Saved, int Missed, int Skipped, int Cleared);
