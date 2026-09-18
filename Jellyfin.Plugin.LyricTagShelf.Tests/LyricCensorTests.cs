using Jellyfin.Plugin.LyricTagShelf;
using Jellyfin.Plugin.LyricTagShelf.Configuration;
using Xunit;

namespace Jellyfin.Plugin.LyricTagShelf.Tests;

public class LyricCensorTests
{
    [Fact]
    public void Glass_IsNotCensored_WhenAssIsOnBlacklist()
    {
        var lrc = "[00:01.00]through the glass window";
        var result = LyricCensor.Apply(lrc, CensorMode.Full, CensorSymbolStyle.Asterisks, "ass\nasshole\n");
        Assert.Equal(lrc, result);
    }

    [Fact]
    public void Asshole_IsCensored_OnlyWhenListed()
    {
        var lrc = "[00:01.00]you asshole";
        var without = LyricCensor.Apply(lrc, CensorMode.Full, CensorSymbolStyle.Asterisks, "ass\n");
        Assert.Equal(lrc, without);

        var with = LyricCensor.Apply(lrc, CensorMode.Full, CensorSymbolStyle.Asterisks, "ass\nasshole\n");
        Assert.Equal("[00:01.00]you *******", with);
    }

    [Fact]
    public void HyphenSplitsWords_SoEachSideMustBeListed()
    {
        var lrc = "[00:01.00]mother-fucker";
        var onlyFuck = LyricCensor.Apply(lrc, CensorMode.Full, CensorSymbolStyle.Asterisks, "fuck\n");
        Assert.Equal(lrc, onlyFuck);

        var fucker = LyricCensor.Apply(lrc, CensorMode.Full, CensorSymbolStyle.Asterisks, "fucker\n");
        Assert.Equal("[00:01.00]mother-******", fucker);

        var compound = LyricCensor.Apply(lrc, CensorMode.Full, CensorSymbolStyle.Asterisks, "motherfucker\n");
        Assert.Equal(lrc, compound);
    }

    [Fact]
    public void ClientCanAddCustomWord()
    {
        var lrc = "[00:01.00]what a dingus";
        var result = LyricCensor.Apply(lrc, CensorMode.Ending, CensorSymbolStyle.Asterisks, "dingus\n");
        Assert.Equal("[00:01.00]what a d*****", result);
    }

    [Fact]
    public void TimestampsPreserved()
    {
        var lrc = "[ar:Test]\n[00:12.34]fuck this shit";
        var result = LyricCensor.Apply(lrc, CensorMode.Full, CensorSymbolStyle.Asterisks, "fuck\nshit\n");
        Assert.Equal("[ar:Test]\n[00:12.34]**** this ****", result);
    }

    [Fact]
    public void LegacyRootMode_ActsLikeEnding()
    {
        var lrc = "[00:01.00]fucking hell";
        var root = LyricCensor.Apply(lrc, CensorMode.Root, CensorSymbolStyle.Asterisks, "fucking\n");
        var ending = LyricCensor.Apply(lrc, CensorMode.Ending, CensorSymbolStyle.Asterisks, "fucking\n");
        Assert.Equal(ending, root);
        Assert.Equal("[00:01.00]f****** hell", root);
    }

    [Fact]
    public void ParseLists_IgnoresWhitelistSection()
    {
        var lists = LyricCensor.ParseLists("# Blacklist\nfuck\n# Whitelist\nfuck\n");
        Assert.True(lists.Blacklist.Contains("fuck"));
        var lrc = "[00:01.00]fuck";
        var result = LyricCensor.Apply(lrc, CensorMode.Full, CensorSymbolStyle.Asterisks, "# Blacklist\nfuck\n# Whitelist\nfuck\n");
        Assert.Equal("[00:01.00]****", result);
    }

    [Fact]
    public void TrailingApostropheInForms_MatchIngListEntries()
    {
        // Older saved lists often have "fucking" / "motherfucking" but not the -in spellings.
        var words = "fucking\nmotherfucking\nbitching\n";
        var fuckin = LyricCensor.Apply("[00:01.00]fuckin' hell", CensorMode.Full, CensorSymbolStyle.Asterisks, words);
        Assert.Equal("[00:01.00]******' hell", fuckin);

        var mf = LyricCensor.Apply("[00:01.00]motherfuckin' loud", CensorMode.Full, CensorSymbolStyle.Asterisks, words);
        Assert.DoesNotContain("motherfuckin", mf, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'", mf);
    }

    [Fact]
    public void TrailingApostropheInForms_AreCensored()
    {
        var words = "fuckin\nfucking\nmotherfuckin\nmotherfucking\nbitchin\nbitching\nshittin\nshitting\n";
        var fuckin = LyricCensor.Apply("[00:01.00]fuckin' hell", CensorMode.Full, CensorSymbolStyle.Asterisks, words);
        Assert.Equal("[00:01.00]******' hell", fuckin);

        var curly = LyricCensor.Apply("[00:01.00]motherfuckin’ yeah", CensorMode.Full, CensorSymbolStyle.Asterisks, words);
        Assert.DoesNotContain("motherfuckin", curly, StringComparison.OrdinalIgnoreCase);

        var bitchin = LyricCensor.Apply("[00:01.00]bitchin'", CensorMode.Full, CensorSymbolStyle.Asterisks, words);
        Assert.Equal("[00:01.00]*******'", bitchin);
    }

    [Fact]
    public void PossessiveShit_IsCensored()
    {
        var result = LyricCensor.Apply(
            "[00:01.00]this shit's crazy",
            CensorMode.Full,
            CensorSymbolStyle.Asterisks,
            "shit\n");
        Assert.Equal("[00:01.00]this ****'s crazy", result);
    }

    [Fact]
    public void DefaultBlacklist_IncludesMotherfuckin()
    {
        Assert.Contains("motherfuckin", LyricCensor.DefaultBlacklist);
        var result = LyricCensor.Apply(
            "[00:01.00]motherfuckin' loud",
            CensorMode.Full,
            CensorSymbolStyle.Asterisks,
            LyricCensor.DefaultWordListText);
        Assert.DoesNotContain("motherfuckin", result, StringComparison.OrdinalIgnoreCase);
    }
}
