namespace Jellyfin.Plugin.Polyglot.Models;

/// <summary>
/// Determines how mirrored files are linked back to their source.
/// </summary>
public enum LinkMode
{
    /// <summary>
    /// Create hardlinks. Requires source and target to be on the same filesystem.
    /// </summary>
    Hardlink,

    /// <summary>
    /// Create symlinks. Can cross filesystem/mount boundaries, but the symlink
    /// itself must be stored on a filesystem that supports symlinks (e.g. not
    /// exFAT/FAT32 on Linux).
    /// </summary>
    Symlink
}
