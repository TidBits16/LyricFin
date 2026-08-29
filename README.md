<p align="center">
  <img src="logo.svg" alt="LyricFin" width="128" height="128">
</p>

# LyricFin: Get Timed Lyrics

<p align="center">
  <img src="backdrop.svg" alt="LyricFin backdrop" width="100%">
</p>

A Jellyfin plugin that fetches **timed LRC** lyrics from [LRCLIB](https://lrclib.net/) - synced only, no plain-text fallback.

**Jellyfin 10.11+** · scheduled task + one settings button.

## Why

Jellyfin’s stock LrcLib provider often leaves you without usable timed lyrics. LyricFin talks to LRCLIB directly, keeps only `syncedLyrics`, and writes them as `.lrc` through Jellyfin’s lyric manager.

## How it works

1. **Scheduled task** (`LyricFin: Get Timed Lyrics`) - fills tracks that are **missing** lyrics.
2. **Settings → Fetch all lyrics** - queues `LyricFin: Fetch All Lyrics` (force overwrite). Runs as a scheduled task so the browser request cannot time out.
3. Lookup order: LRCLIB `/api/get` (with duration) → get without album → `/api/search` for the best synced match.
4. ExplicitFin-style marks (`🅴`, `[Explicit]`, …) are stripped from titles before searching (configurable, same ignore list as MusicFin).
5. **Skip (Instrumental) titles** (on by default): scheduled runs ignore `(Instrumental)` / `[Instrumental]`; force fetch **clears** lyrics on those tracks.

## Providers

LRCLIB is the best free, no-key option for synced LRC and is what LyricFin uses. Alternatives exist (Musixmatch, NetEase, Megalobiz via scrapers, Spotify/Musixmatch proxies) but usually need API keys, ToS risk, or brittle scraping — not wired in yet.

## Install

1. **Dashboard → Plugins → Repositories** → add:
   - Name: `FinPlugins`
   - URL: `https://raw.githubusercontent.com/TidBits16/FinPlugins/main/manifest.json`
2. **Catalog** → refresh → install **LyricFin: Get Timed Lyrics** → restart when asked.
3. Configure under **Plugins → LyricFin: Get Timed Lyrics**, or run from **Scheduled Tasks**.

(That same repository URL also lists the other Fin plugins: MusicFin, ExplicitFin, LyricFin, and ArtistFin.)

## Build locally

```bash
dotnet build Jellyfin.Plugin.LyricFin.csproj -c Release
./scripts/package.sh
```

The release zip lands in `dist/`.
