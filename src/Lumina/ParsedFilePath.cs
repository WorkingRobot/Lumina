using Lumina.Data;
using Lumina.Extensions;

namespace Lumina;

/// <summary>
/// Represents a parsed file path, containing the repository, category, and index hashes for a given file path.
/// </summary>
public record ParsedFilePath
{
    /// <summary>
    /// The repository (or expansion) that a file belongs to.
    ///
    /// e.g. ffxiv, ex1, ex2, ...
    /// </summary>
    public string Repository { get; }

    /// <summary>
    /// The category of the file, essentially it's 'container'
    ///
    /// bg, game_script, etc.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Index hash
    /// </summary>
    public ulong IndexHash { get; }

    /// <summary>
    /// Index2 hash
    /// </summary>
    public uint Index2Hash { get; }

    /// <summary>
    /// The portion of an <see cref="IndexHash"/> that represents the folders in the path only
    /// </summary>
    public uint FolderHash => (uint)( IndexHash >> 32 );

    /// <summary>
    /// The portion of an <see cref="IndexHash"/> that represents the filename in the path only
    /// </summary>
    public uint FileHash => (uint)IndexHash;

    /// <summary>
    /// The raw path provided when parsing the initial path
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Creates a new <see cref="ParsedFilePath"/> from a raw path string.
    /// </summary>
    /// <param name="path">A path to a file located inside the game's filesystem.</param>
    public ParsedFilePath( string path )
    {
        var hashedPath = HashedFilePath.Create( path, out var repo );
        Path = path;
        Repository = repo;
        Category = hashedPath.Category.GetName();
        IndexHash = hashedPath.OldHash;
        Index2Hash = hashedPath.Hash;
    }

    public static implicit operator string( ParsedFilePath obj ) => obj.Path;
}