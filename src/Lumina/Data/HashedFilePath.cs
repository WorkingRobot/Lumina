using Lumina.Extensions;
using System;
using System.IO.Hashing;
using System.Text;
using static Lumina.Data.SqPackVfs;

namespace Lumina.Data;

public readonly record struct HashedFilePath( byte ExpansionId, CategoryType Category, uint Hash, ulong OldHash )
{
    internal readonly SqPackKey PackKey => new( ExpansionId, Category );

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

        byte expansionId = 0;
        int splitTwo = path.IndexOf( '/', splitOne + 1 );
        if( splitTwo != -1 )
        {
            repository = path[( splitOne + 1 )..splitTwo];
            if( repository.StartsWith( "ex" ) )
                _ = byte.TryParse( repository[2..], out expansionId );
        }
        else
            repository = "ffxiv";

        Span<byte> pathBytes = stackalloc byte[240];
        Span<byte> pathData = pathBytes[..Encoding.UTF8.GetBytes( path, pathBytes )];
        uint hash = ~Crc32.HashToUInt32( pathData );

        int folderSeparator = path.LastIndexOf( '/' );
        Span<byte> folderData = pathBytes[..Encoding.UTF8.GetBytes( path.AsSpan( ..folderSeparator ), pathBytes )];
        uint folderHash = ~Crc32.HashToUInt32( folderData );
        Span<byte> fileData = pathBytes[..Encoding.UTF8.GetBytes( path.AsSpan( ( folderSeparator + 1 ).. ), pathBytes )];
        uint fileHash = ~Crc32.HashToUInt32( fileData );
        ulong oldHash = (ulong)folderHash << 32 | fileHash;

        return new( expansionId, categoryType, hash, oldHash );
    }
}
