<div align="center">

<p align="center">
  <img src="logo.svg" alt="LyricFin" width="128" height="128">
</p>

# LyricFin: Get Timed Lyrics

<p align="center">
  <img src="backdrop.svg" alt="LyricFin backdrop" width="100%">
</p>

A Jellyfin plugin that fetches <strong>timed LRC</strong> lyrics from <a href="https://lrclib.net/">LRCLIB</a> - synced only, no plain-text fallback.

<strong>Jellyfin 10.11+</strong> · scheduled task + one settings button.

## Why

Jellyfin’s stock LrcLib provider often leaves you without usable timed lyrics. LyricFin talks to LRCLIB directly, keeps only `syncedLyrics`, and writes them as `.lrc` through Jellyfin’s lyric manager.

## How it works

<strong>Scheduled task</strong> (`LyricFin: Get Timed Lyrics`) - fills tracks that are <strong>missing</strong> lyrics.
<strong>Settings --> Fetch all lyrics</strong> - queues `LyricFin: Fetch All Lyrics` (force overwrite). Runs as a scheduled task so the browser request cannot time out.
Lookup order: LRCLIB `/api/get` (with duration) --> get without album --> `/api/search` for the best synced match.
ExplicitFin-style marks (`🅴`, `[Explicit]`, …) are stripped from titles before searching (configurable, same ignore list as MusicFin).
<strong>Skip (Instrumental) titles</strong> (on by default): scheduled runs ignore `(Instrumental)` / `[Instrumental]`; force fetch <strong>clears</strong> lyrics on those tracks.

## Providers

LRCLIB is the best free, no-key option for synced LRC and is what LyricFin uses. Alternatives exist (Musixmatch, NetEase, Megalobiz via scrapers, Spotify/Musixmatch proxies) but usually need API keys, ToS risk, or brittle scraping - not wired in yet.

## Installing
<strong>Step 1</strong>
<p align="center">
  <img src="repo_graphics/plugins.jpg" alt="Plugins Location" width="100%">
</p>

<strong>Dashboard --> Plugins --> Manage Repositories</strong> --> <strong>+ New Repository</strong>:<br>
Name: <code>FinPlugins</code> (or whatever :P )<br>
URL: <code>https://raw.githubusercontent.com/TidBits16/FinPlugins/main/manifest.json</code><br>
<br>
(p.s. this bundle includes my other FinPlugins since they are designed to work together. <strong><em>they are not required to install!</em></strong>)
<br>
<br>
<strong>Then Restart JellyFin!</strong>

<strong>Step 2</strong>
<p align="center">
  <img src="repo_graphics/where_to_find.jpg" alt="Where To Find Repo" width="100%">
</p>

<strong>Plugins</strong> --> <strong>All</strong> --> <strong>LyricFin: Get Timed Lyrics</strong> --> <strong>Install</strong><br>
<br>
<strong>Once Installed, Restart JellyFin Again!</strong></center>

## Build Locally

For development or packaging your own build:

```bash
dotnet build Jellyfin.Plugin.LyricFin.csproj -c Release
./scripts/package.sh
```

The release zip will be in `dist/`.

Designed for <strong>Jellyfin 10.11+</strong> (you probably have this already :D)
<p align="center">
  <a href="https://github.com/TidBits16/MusicFin"><img src="repo_graphics/musicfin.svg" alt="MusicFin" width="72" height="72"></a>
  &nbsp;
  <a href="https://github.com/TidBits16/ExplicitFin"><img src="repo_graphics/explicitfin.svg" alt="ExplicitFin" width="72" height="72"></a>
  &nbsp;
  <a href="https://github.com/TidBits16/LyricFin"><img src="repo_graphics/lyricfin.svg" alt="LyricFin" width="72" height="72"></a>
  &nbsp;
  <a href="https://github.com/TidBits16/ArtistFin"><img src="repo_graphics/artistfin.svg" alt="ArtistFin" width="72" height="72"></a>
</p>
</div>
