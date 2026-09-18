namespace Jellyfin.Plugin.LyricTagShelf.Configuration;

public enum CensorMode
{
    /// <summary>Leave lyrics unchanged.</summary>
    None = 0,

    /// <summary>Keep the first letter, mask the rest (F***** for fucker).</summary>
    Ending = 1,

    /// <summary>Mask the whole word (****** for fucker).</summary>
    Full = 2,

    /// <summary>Legacy alias for Ending (whole-word first-letter mask).</summary>
    Root = 3,
}
