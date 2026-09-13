using Lumina.Data.Structs.SqPack;
using Lumina.Misc;
using System;
using System.IO;
using System.IO.Compression;

namespace Lumina.Data;

public abstract class SqPackStream : Stream
{
    public MappedFile DatFile { get; }

    public long Offset { get; }

    public SqPackFileHeader Header { get; }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => Header.UncompressedFileSize;

    protected internal SqPackStream( MappedFile datFile, long offset )
    {
        DatFile = datFile;
        Offset = offset;
        Header = datFile.GetAt<SqPackFileHeader>( offset );
    }

    protected (MappedFile.Segment<byte> Data, int? DecompressedSize) GetBlockInfo( long offset )
    {
        long blockOffset = Offset + Header.HeaderSize + offset;
        SqPackBlockHeader blockHeader = DatFile.GetAt<SqPackBlockHeader>( blockOffset );
        if( blockHeader.DataSize == 0 )
            throw new InvalidOperationException( "Empty block" );

        long dataOffset = blockOffset + blockHeader.HeaderSize;
        if( blockHeader.CompressedSize == 32000 ) // Uncompressed
            return (DatFile.GetSpanAt<byte>( dataOffset, checked((int)blockHeader.DataSize) ), null);
        else
            return (DatFile.GetSpanAt<byte>( dataOffset, checked((int)blockHeader.CompressedSize) ), checked((int)blockHeader.DataSize));
    }

    protected readonly record struct BlockData( byte[]? RawData, MappedFile.Segment<byte>? FileData )
    {
        public BlockData( byte[] rawData ) : this( rawData, null ) { }

        public BlockData( MappedFile.Segment<byte> fileData ) : this( null, fileData ) { }

        public ReadOnlySpan<byte> Span =>
            RawData == null ? FileData!.Value.Span : RawData.AsSpan();
    }

    protected BlockData GetBlock( long offset )
    {
        (MappedFile.Segment<byte> data, int? uncompSize) = GetBlockInfo( offset );
        if( uncompSize.HasValue )
        {
            byte[] ret = new byte[uncompSize.Value];

            using UnmanagedMemoryStream stream = data.Stream;
            using DeflateStream zlibStream = new( stream, CompressionMode.Decompress );
            zlibStream.ReadExactly( ret );

            return new(ret);
        }
        else
            return new(data);
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override void SetLength( long value )
    {
        throw new NotSupportedException();
    }

    public override void Write( byte[] buffer, int offset, int count )
    {
        throw new NotSupportedException();
    }

    public static SqPackStream Create( MappedFile datFile, long offset, GameData gameData, ulong indexHash )
    {
        ref readonly SqPackFileHeader header = ref datFile.GetAt<SqPackFileHeader>( offset );
        return header.Type switch
        {
            SqPackFileType.Empty => new SqPackEmptyStream( datFile, offset, gameData, indexHash ),
            SqPackFileType.Standard => new SqPackStandardStream( datFile, offset ),
            SqPackFileType.Model => new SqPackModelStream( datFile, offset ),
            SqPackFileType.Texture => new SqPackTextureStream( datFile, offset ),
            _ => throw new InvalidDataException( "Invalid header type" )
        };
    }
}
