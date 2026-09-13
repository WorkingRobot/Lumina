using Lumina.Data.Structs.SqPack;
using Lumina.Misc;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lumina.Data;

public sealed class SqPackStandardStream : SqPackStream
{
    public new SqPackFileStandardHeader Header { get; }

    public override long Position { get => BytePosition; set => Seek( value, SeekOrigin.Begin ); }

    private long BytePosition { get; set; }
    private int BlockIndex { get; set; }
    private int BlockOffset { get; set; }
    private BlockData? BlockInfo { get; set; }

    private MappedFile.Segment<SqPackStandardBlockDescriptor> StandardBlockDescriptors { get; }
    private long[]? BlockStartPositions { get; }
    private ushort? UniformBlockUncompressedSize { get; }

    public SqPackStandardStream( MappedFile datFile, long offset ) : base( datFile, offset )
    {
        Header = datFile.GetAt<SqPackFileStandardHeader>( offset );
        StandardBlockDescriptors = DatFile.GetSpanAt<SqPackStandardBlockDescriptor>( Offset + Unsafe.SizeOf<SqPackFileStandardHeader>(), checked((int)Header.BlockCount) );
        BlockStartPositions = new long[StandardBlockDescriptors.Length];
        long pos = 0;
        for( int i = 0; i < StandardBlockDescriptors.Length; ++i )
        {
            BlockStartPositions[i] = pos;
            ushort uncompSize = StandardBlockDescriptors.Span[i].UncompressedSize;
            pos += uncompSize;
            if( i == 0 )
                UniformBlockUncompressedSize = uncompSize;
            else if( UniformBlockUncompressedSize != null && UniformBlockUncompressedSize != uncompSize && i != StandardBlockDescriptors.Length - 1 )
                UniformBlockUncompressedSize = null;
        }
        if( UniformBlockUncompressedSize.HasValue )
            BlockStartPositions = null;
    }

    public override int Read( byte[] buffer, int offset, int count )
    {
        Span<byte> b = buffer.AsSpan().Slice( offset, count );
        while( !b.IsEmpty )
        {
            if (GetBlock() is not { } blockData )
                break;
            var data = blockData.Span;
            int toCopy = Math.Min( data.Length - BlockOffset, b.Length );
            data.Slice( BlockOffset, toCopy ).CopyTo( b );
            b = b[toCopy..];

            BlockOffset += toCopy;
            BytePosition += toCopy;
            if( BlockOffset >= data.Length )
            {
                BlockIndex++;
                BlockOffset = 0;
                BlockInfo = null;
            }
        }
        return count - b.Length;
    }

    public override long Seek( long offset, SeekOrigin origin )
    {
        offset = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => BytePosition + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException( nameof( origin ) )
        };

        (int idx, BlockOffset) = ResolveBlockPosition( offset );
        if( idx != BlockIndex )
        {
            BlockIndex = idx;
            BlockInfo = null;
        }

        return BytePosition = offset;
    }

    private BlockData? GetBlock()
    {
        if( !BlockInfo.HasValue )
        {
            if( StandardBlockDescriptors.Length <= BlockIndex )
                return null;
            BlockInfo = GetBlock( StandardBlockDescriptors.Span[BlockIndex].Offset );
        }
        return BlockInfo.Value;
    }

    private (int BlockIndex, int BlockOffset) ResolveBlockPosition( long offset )
    {
        if( UniformBlockUncompressedSize.HasValue )
        {
            (long a, long b) = long.DivRem( offset, UniformBlockUncompressedSize.Value );
            return checked(((int)a, (int)b));
        }

        if( BlockStartPositions == null || BlockStartPositions.Length == 0 )
            throw new InvalidOperationException( "No blocks" );

        // Binary search for the block index
        int idx = BlockStartPositions.AsSpan().BinarySearch( offset );
        if( idx < 0 )
            idx = ~idx - 1;

        return checked((idx, (int)( offset - BlockStartPositions[idx] )));
    }
}
