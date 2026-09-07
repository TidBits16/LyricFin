namespace Jellyfin.Plugin.LyricFin.Configuration;

public enum CensorMode
{
    /// <summary>Leave lyrics unchanged.</summary>
    None = 0,

    /// <summary>Keep the first letter, mask the rest (F***** for fucker).</summary>
    Ending = 1,

    /// <summary>Mask the whole word (****** for fucker).</summary>
    Full = 2,

    /// <summary>Mask a blacklist stem inside a word (F***er / motherF***ing).</summary>
    Root = 3,
}
