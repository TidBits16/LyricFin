<div align="center">
<p align="center">
  <img src="backdrop.svg" alt="LyricTagShelf backdrop" width="100%">
</p>

# LyricTagShelf: Get Timed Lyrics

> <strong>LLM disclosure:</strong> This plugin is <strong>primarily developed with LLM assistance</strong> (Cursor / coding agents). Review and test before relying on it in production.

> Formerly <strong>LyricFin</strong>. Same plugin GUID — settings carry over when you update.

I found that the stock "LrcLib Lyrics" plugin struggles to fetch timed lyrics. It also has difficulty identifying songs so most of my collection was missing lyrics.
<br>
<strong>That's why I created my own lyric plugin.</strong>
<br>
<p align="center">
  <img src="repo_graphics/lyrictagshelf_meme.jpg" alt="LyricTagShelf Meme" width="100%">
</p>

LyricTagShelf prefers <strong>timed LRC</strong> lyrics from <a href="https://lrclib.net/">LRCLIB</a> and also supports fallbacks to make sure all your tracks get identified.

It'll also remove lyrics on instrumental tracks *(can be disabled for the karaoke fans)!*

And, it works with explicit symbols in the title (`🅴`, `[Explicit]`, ...)!

***Please sing responsibly!***


## Providers

LRCLIB is the best free, no-key option for synced LRC and is what LyricTagShelf uses by default. You can also use Musixmatch, NetEase, but they usually require keys (and I haven't come across many songs that these two cannot find lyrics for).

## Installing
<strong>Step 1</strong>
<p align="center">
  <img src="repo_graphics/plugins.jpg" alt="Plugins Location" width="100%">
</p>

<strong>Dashboard --> Plugins --> Manage Repositories</strong> --> <strong>+ New Repository</strong>:<br>
Name: <code>TagShelfPlugins</code> (or whatever :P )<br>
URL: <code>https://raw.githubusercontent.com/TidBits16/TagShelfPlugins/main/manifest.json</code><br>
<br>
(p.s. this bundle includes my other TagShelfPlugins since they are designed to work together. <strong><em>they are not required to install!</em></strong>)<br>
For just <strong>LyricTagShelf</strong> you can use this URL: <code>https://raw.githubusercontent.com/TidBits16/LyricTagShelf/main/manifest.json</code>
<br>
<br>
<strong>Then Restart Jellyfin!</strong>

<strong>Step 2</strong>
<p align="center">
  <img src="repo_graphics/where_to_find.jpg" alt="Where To Find Repo" width="100%">
</p>

<strong>Plugins</strong> --> <strong>All</strong> --> <strong>LyricTagShelf: Get Timed Lyrics</strong> --> <strong>Install</strong><br>
<br>
<strong>Once Installed, Restart Jellyfin Again!</strong></center>

## Build Locally

For development or packaging your own build:

```bash
dotnet build Jellyfin.Plugin.LyricTagShelf.csproj -c Release
./scripts/package.sh
```

The release zip will be in `dist/`.

Designed for <strong>Jellyfin 10.11+</strong> (you probably have this already :D)
<br>
Licensed under the <a href="LICENSE">GNU General Public License v3.0</a>
<p align="center">
  <a href="https://github.com/TidBits16/MusicTagShelf"><img src="repo_graphics/musictagshelf.svg" alt="MusicTagShelf" width="72" height="72"></a>
  &nbsp;
  <a href="https://github.com/TidBits16/ExplicitTagShelf"><img src="repo_graphics/explicittagshelf.svg" alt="ExplicitTagShelf" width="72" height="72"></a>
  &nbsp;
  <a href="https://github.com/TidBits16/LyricTagShelf"><img src="repo_graphics/lyrictagshelf.svg" alt="LyricTagShelf" width="72" height="72"></a>
  &nbsp;
  <a href="https://github.com/TidBits16/ArtistTagShelf"><img src="repo_graphics/artisttagshelf.svg" alt="ArtistTagShelf" width="72" height="72"></a>
</p>
</div>
