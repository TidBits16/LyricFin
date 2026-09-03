<div align="center">
<p align="center">
  <img src="backdrop.svg" alt="LyricFin backdrop" width="100%">
</p>

# LyricFin: Get Timed Lyrics

I found that the stock "LrcLib Lyrics" plugin struggles to fetch timed lyrics. It also has difficulty identifying songs so most of my collection was missing lyrics.
<br>
<strong>That's why I created my own lyric plugin.</strong>
<br>
<p align="center">
  <img src="repo_graphics/lyricfin_meme.jpg" alt="LyricFin Meme" width="100%">
</p>

LyricFin prefers <strong>timed LRC</strong> lyrics from <a href="https://lrclib.net/">LRCLIB</a> and also supports fallbacks to make sure all your tracks get identified.

It'll also remove lyrics on instrumental tracks *(can be disabled for the karaoke fans)!*

And, it works with explicit symbols in the title (`🅴`, `[Explicit]`, ...)!

***Please sing responsibly!***


## Providers

LRCLIB is the best free, no-key option for synced LRC and is what LyricFin uses by default. You can also use Musixmatch, NetEase, but they usually require keys (and I haven't come across many songs that these two cannot find lyrics for).

## Installing
<strong>Step 1</strong>
<p align="center">
  <img src="repo_graphics/plugins.jpg" alt="Plugins Location" width="100%">
</p>

<strong>Dashboard --> Plugins --> Manage Repositories</strong> --> <strong>+ New Repository</strong>:<br>
Name: <code>FinPlugins</code> (or whatever :P )<br>
URL: <code>https://raw.githubusercontent.com/TidBits16/FinPlugins/main/manifest.json</code><br>
<br>
(p.s. this bundle includes my other FinPlugins since they are designed to work together. <strong><em>they are not required to install!</em></strong>)<br>
For just <strong>LyricFin</strong> you can use this URL: <code>https://raw.githubusercontent.com/TidBits16/LyricFin/main/manifest.json</code>
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
<br>
Licensed under the <a href="LICENSE">GNU General Public License v3.0</a>
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
