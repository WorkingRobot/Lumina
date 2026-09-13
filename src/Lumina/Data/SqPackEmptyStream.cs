using Lumina.Data.Structs.SqPack;
using Lumina.Misc;
using System;
using System.IO;
using System.IO.Compression;

namespace Lumina.Data;

public sealed class SqPackEmptyStream : SqPackStream
{
    public new SqPackFileEmptyHeader Header { get; }

    private byte[] Data { get; }

    public override long Position { get; set; }

    public SqPackEmptyStream( MappedFile datFile, long offset, GameData gameData, ulong indexHash ) : base( datFile, offset )
    {
        Header = datFile.GetAt<SqPackFileEmptyHeader>( offset );

        Data = new byte[Header.Base.UncompressedFileSize];

        var compressedData = DatFile.GetSpanAt<byte>( Offset + Header.Base.HeaderSize, checked((int)Header.CompressedFileSize) ).Span.ToArray();

        if( !( gameData.Options.RsfResolver?.Invoke( indexHash, out var resolvedHeader ) ?? false ) )
            gameData.Options.Logger?.Error( $"Failed to resolve RSF with {indexHash:X16} index hash." );
        else
            resolvedHeader.CopyTo( compressedData );

        using MemoryStream stream = new( compressedData, false );
        using ZLibStream zlibStream = new( stream, CompressionMode.Decompress );
        zlibStream.ReadExactly( Data );
    }

    public override int Read( byte[] buffer, int offset, int count )
    {
        Span<byte> b = buffer.AsSpan().Slice( offset, count );

        if( Length <= Position )
            return 0;

        var toCopy = Math.Min( checked((int)(Length - Position)), b.Length );
        Data.AsSpan().Slice( checked((int)Position), toCopy ).CopyTo( b );
        Position += toCopy;
        return toCopy;
    }

    public override long Seek( long offset, SeekOrigin origin )
    {
        offset = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException( nameof( origin ) )
        };
        return Position = offset;
    }
}
