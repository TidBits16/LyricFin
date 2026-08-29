# LyricFin: Get Timed Lyrics

A Jellyfin plugin that fetches **timed LRC** lyrics from [LRCLIB](https://lrclib.net/) - synced only, no plain-text fallback.

**Jellyfin 10.11+** · scheduled task + one settings button.

## Why

Jellyfin’s stock LrcLib provider often leaves you without usable timed lyrics. LyricFin talks to LRCLIB directly, keeps only `syncedLyrics`, and writes them as `.lrc` through Jellyfin’s lyric manager.

## How it works

1. **Scheduled task** (`LyricFin: Get Timed Lyrics`) - fills tracks that are **missing** lyrics.
2. **Settings → Fetch all lyrics** - force-refetch for every track (overwrites when timed LRC is found).
3. Lookup order: LRCLIB `/api/get` (with duration) → get without album → `/api/search` for the best synced match.
4. ExplicitFin-style marks (`🅴`, `[Explicit]`, …) are stripped from titles before searching.

## Install

1. **Dashboard → Plugins → Repositories** → add:
   - Name: `FinPlugins`
   - URL: `https://raw.githubusercontent.com/TidBits16/FinPlugins/main/manifest.json`
2. **Catalog** → refresh → install **LyricFin: Get Timed Lyrics** → restart when asked.
3. Configure under **Plugins → LyricFin: Get Timed Lyrics**, or run from **Scheduled Tasks**.

(That same repository URL also lists MusicFin and ExplicitFin.)

## Build locally

```bash
dotnet build Jellyfin.Plugin.LyricFin.csproj -c Release
./scripts/package.sh
```

The release zip lands in `dist/`.
