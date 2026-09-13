using Lumina.Data;
using Lumina.Extensions;

namespace Lumina;

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
    public uint FolderHash => (uint)(IndexHash >> 32);

    /// <summary>
    /// The portion of an <see cref="IndexHash"/> that represents the filename in the path only
    /// </summary>
    public uint FileHash => (uint)IndexHash;
    
    /// <summary>
    /// The raw path provided when parsing the initial path
    /// </summary>
    public string Path { get; }

    public ParsedFilePath(string path)
    {
        var (_, category, hash, oldHash) = HashedFilePath.Create( path, out var repo );
        Path = path;
        Repository = repo;
        Category = category.GetName();
        IndexHash = oldHash;
        Index2Hash = hash;
    }
    
    public static implicit operator string( ParsedFilePath obj ) => obj.Path;
}