namespace Jellyfin.Plugin.LyricFin.Configuration;

public enum CensorMode
{
    /// <summary>Leave lyrics unchanged.</summary>
    None = 0,

    /// <summary>Keep the first letter, mask the rest (f***).</summary>
    Ending = 1,

    /// <summary>Mask the whole word (****).</summary>
    Full = 2,
}
