using Lumina.Extensions;
using System;
using System.IO.Hashing;
using System.Text;
using static Lumina.Data.SqPackVfs;

namespace Lumina.Data;

public readonly record struct HashedFilePath( byte ExpansionId, CategoryType Category, uint Hash, int FolderSeparator, string Path )
{
    internal readonly SqPackKey PackKey => new( ExpansionId, Category );

    /// <summary>
    /// The old hash of the file path, computed on demand. This is the same hash that's used for resolving RSFs.
    /// </summary>
    public ulong OldHash
    {
        get
        {
            Span<byte> pathBytes = stackalloc byte[240];
            var folderData = pathBytes[..Encoding.UTF8.GetBytes( Path.AsSpan( ..FolderSeparator ), pathBytes )];
            var folderHash = ~Crc32.HashToUInt32( folderData );
            var fileData = pathBytes[..Encoding.UTF8.GetBytes( Path.AsSpan( ( FolderSeparator + 1 ).. ), pathBytes )];
            var fileHash = ~Crc32.HashToUInt32( fileData );
            return (ulong)folderHash << 32 | fileHash;
        }
    }

    public static HashedFilePath Create( string path, out string repository )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( path );
        if( path[^1] == '/' )
        {
            throw new ArgumentException( "Path cannot end with a '/'", nameof( path ) );
        }

        path = path.ToLowerInvariant().Trim();

        if( path.Length > 240 )
            throw new ArgumentOutOfRangeException( nameof( path ), "Path is too long" );

        int splitOne = path.IndexOf( '/' );
        ArgumentOutOfRangeException.ThrowIfNegative( splitOne, nameof( path ) );

        string category = path[..splitOne];
        CategoryType categoryType = CategoryExtensions.GetCategoryType( category );

        // only an "exN" is a repository; anything else is a folder inside ffxiv
        byte expansionId = 0;
        repository = "ffxiv";
        int splitTwo = path.IndexOf( '/', splitOne + 1 );
        if( splitTwo != -1 )
        {
            var segment = path[( splitOne + 1 )..splitTwo];
            if( segment.StartsWith( "ex" ) && byte.TryParse( segment[2..], out expansionId ) )
                repository = segment;
        }

        Span<byte> pathBytes = stackalloc byte[240];
        Span<byte> pathData = pathBytes[..Encoding.UTF8.GetBytes( path, pathBytes )];
        uint hash = ~Crc32.HashToUInt32( pathData );

        return new( expansionId, categoryType, hash, path.LastIndexOf( '/' ), path );
    }
}
