using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.LyricFin;

public sealed class LrcHit
{
    public string SyncedLyrics { get; init; } = string.Empty;

    public string Source { get; init; } = "lrclib";
}

public sealed class LrcLibClient
{
    private const string Base = "https://lrclib.net";
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(14);

    private readonly PacedHttp _http;
    private readonly ILogger<LrcLibClient> _logger;

    public LrcLibClient(IHttpClientFactory factory, HttpCache cache, ILogger<LrcLibClient> logger)
    {
        _http = new PacedHttp(
            factory,
            cache,
            TimeSpan.FromMilliseconds(350),
            "LyricFin/1.0.0 (https://github.com/TidBits16/LyricFin)");
        _logger = logger;
    }

    public int HttpCount => _http.HttpCount;

    public int CacheHits => _http.CacheHits;

    /// <summary>
    /// Finds synced LRC for a track. Prefers exact /api/get, then search for best timed match.
    /// </summary>
    public async Task<LrcHit?> FindSyncedAsync(
        string title,
        string artist,
        string album,
        double? durationSeconds,
        CancellationToken cancellationToken)
    {
        if (title.Length == 0 || artist.Length == 0)
        {
            return null;
        }

        // 1) Exact signature match (duration helps a lot on LRCLIB).
        var exact = await GetExactAsync(title, artist, album, durationSeconds, cancellationToken)
            .ConfigureAwait(false);
        if (exact is not null)
        {
            return exact;
        }

        // 2) Exact without album (some tags are messy).
        if (album.Length > 0)
        {
            exact = await GetExactAsync(title, artist, string.Empty, durationSeconds, cancellationToken)
                .ConfigureAwait(false);
            if (exact is not null)
            {
                return exact;
            }
        }

        // 3) Search — pick the best result that has synced lyrics (prefer close duration).
        return await SearchBestSyncedAsync(title, artist, album, durationSeconds, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<LrcHit?> GetExactAsync(
        string title,
        string artist,
        string album,
        double? durationSeconds,
        CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string>
        {
            ["track_name"] = title,
            ["artist_name"] = artist
        };
        if (album.Length > 0)
        {
            query["album_name"] = album;
        }

        if (durationSeconds is > 0 and <= 3600)
        {
            query["duration"] = durationSeconds.Value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        JsonElement? payload;
        try
        {
            payload = await _http.GetJsonAsync(
                "lrclib/get",
                Base + "/api/get",
                query,
                Ttl,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LRCLIB get failed for {Title}", title);
            return null;
        }

        return SyncedFrom(payload, "lrclib-get");
    }

    private async Task<LrcHit?> SearchBestSyncedAsync(
        string title,
        string artist,
        string album,
        double? durationSeconds,
        CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string>
        {
            ["track_name"] = title,
            ["artist_name"] = artist
        };
        if (album.Length > 0)
        {
            query["album_name"] = album;
        }

        JsonElement? payload;
        try
        {
            payload = await _http.GetJsonAsync(
                "lrclib/search",
                Base + "/api/search",
                query,
                Ttl,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LRCLIB search failed for {Title}", title);
            return null;
        }

        if (payload is null || payload.Value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        LrcHit? best = null;
        var bestDelta = double.MaxValue;
        foreach (var item in payload.Value.EnumerateArray())
        {
            var synced = JsonUtil.Str(item, "syncedLyrics").Trim();
            if (synced.Length == 0 || !LooksLikeLrc(synced))
            {
                continue;
            }

            var dur = JsonUtil.Num(item, "duration");
            var delta = durationSeconds is > 0 && dur > 0
                ? Math.Abs(dur - durationSeconds.Value)
                : 999;
            // Prefer closer duration; accept anything with synced lyrics if duration unknown.
            if (delta < bestDelta)
            {
                bestDelta = delta;
                best = new LrcHit { SyncedLyrics = synced, Source = "lrclib-search" };
            }
        }

        // If we have a local duration, require a reasonably close match (±8s) when possible.
        if (best is not null && durationSeconds is > 0 && bestDelta < double.MaxValue && bestDelta > 8)
        {
            // Still return it — better than nothing for force/missing fills — but prefer tighter when available.
            // Only reject extreme mismatches.
            if (bestDelta > 30)
            {
                return null;
            }
        }

        return best;
    }

    private static LrcHit? SyncedFrom(JsonElement? payload, string source)
    {
        if (payload is null || payload.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var synced = JsonUtil.Str(payload.Value, "syncedLyrics").Trim();
        if (synced.Length == 0 || !LooksLikeLrc(synced))
        {
            return null;
        }

        return new LrcHit { SyncedLyrics = synced, Source = source };
    }

    internal static bool LooksLikeLrc(string text)
        => text.Contains('[', StringComparison.Ordinal)
           && text.Contains(']', StringComparison.Ordinal)
           && (text.Contains(':', StringComparison.Ordinal) || text.Contains('.', StringComparison.Ordinal));
}
