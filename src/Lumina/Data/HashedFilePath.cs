using Lumina.Extensions;
using System;
using System.IO.Hashing;
using System.Text;
using static Lumina.Data.SqPackVfs;

namespace Lumina.Data;

public readonly record struct HashedFilePath(
    byte ExpansionId,
    CategoryType Category,
    uint Hash,
    int FolderSeparator,
    string Path )
{
    internal SqPackKey PackKey => new( ExpansionId, Category );

    /// <summary>
    /// The old hash of the file path, computed on demand. This is the same hash that's used for resolving RSFs.
    /// </summary>
    public ulong OldHash {
        get {
            Span< byte > pathBytes = stackalloc byte[240];
            var folderData = pathBytes[ ..Encoding.UTF8.GetBytes( Path.AsSpan( ..FolderSeparator ), pathBytes ) ];
            var folderHash = ~Crc32.HashToUInt32( folderData );
            var fileData = pathBytes[ ..Encoding.UTF8.GetBytes( Path.AsSpan( ( FolderSeparator + 1 ).. ), pathBytes ) ];
            var fileHash = ~Crc32.HashToUInt32( fileData );
            return (ulong)folderHash << 32 | fileHash;
        }
    }

    public static HashedFilePath Create( string path, out string repository )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( path );
        if( path[ ^1 ] == '/' )
        {
            throw new ArgumentException( "Path cannot end with a '/'", nameof( path ) );
        }

        var trimmed = path.AsSpan().Trim();

        if( trimmed.Length > 240 )
            throw new ArgumentOutOfRangeException( nameof( path ), "Path is too long" );

        // lowercasing into a stack buffer = alloc-free
        Span< char > buffer = stackalloc char[240];
        var lowered = buffer[ ..trimmed.ToLowerInvariant( buffer ) ];

        var splitOne = lowered.IndexOf( '/' );
        ArgumentOutOfRangeException.ThrowIfNegative( splitOne, nameof( path ) );

        var categoryType = CategoryExtensions.GetCategoryType( lowered[ ..splitOne ] );

        // only an "exN" is a repository; anything else is a folder inside ffxiv
        byte expansionId = 0;
        repository = "ffxiv";
        var splitTwo = lowered[ ( splitOne + 1 ).. ].IndexOf( '/' );
        if( splitTwo != -1 )
        {
            var segment = lowered.Slice( splitOne + 1, splitTwo );
            if( segment.StartsWith( "ex" ) && byte.TryParse( segment[ 2.. ], out expansionId ) )
                repository = segment.ToString();
        }

        Span< byte > pathBytes = stackalloc byte[240];
        var pathData = pathBytes[ ..Encoding.UTF8.GetBytes( lowered, pathBytes ) ];
        var hash = ~Crc32.HashToUInt32( pathData );

        return new( expansionId, categoryType, hash, lowered.LastIndexOf( '/' ), path );
    }
}
