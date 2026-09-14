using Lumina.Data.Structs.SqPack;
using Lumina.Misc;
using System;
using System.Runtime.CompilerServices;

namespace Lumina.Data;

public sealed class SqPackStandardStream : SqPackStream
{
    public new SqPackFileStandardHeader Header { get; }


    private MappedFile.Segment< SqPackStandardBlockDescriptor > StandardBlockDescriptors { get; }
    private long[]? BlockStartPositions { get; }
    private ushort? UniformBlockUncompressedSize { get; }

    public SqPackStandardStream( MappedFile datFile, long offset ) : base( datFile, offset )
    {
        Header = datFile.GetAt< SqPackFileStandardHeader >( offset );
        StandardBlockDescriptors = DatFile.GetSpanAt< SqPackStandardBlockDescriptor >(
            Offset + Unsafe.SizeOf< SqPackFileStandardHeader >(), checked( (int)Header.BlockCount ) );
        BlockStartPositions = new long[StandardBlockDescriptors.Length];
        long pos = 0;
        for( int i = 0; i < StandardBlockDescriptors.Length; ++i )
        {
            BlockStartPositions[ i ] = pos;
            ushort uncompSize = StandardBlockDescriptors.Span[ i ].UncompressedSize;
            pos += uncompSize;
            if( i == 0 )
                UniformBlockUncompressedSize = uncompSize;
            else if( UniformBlockUncompressedSize != null && UniformBlockUncompressedSize != uncompSize &&
                     i != StandardBlockDescriptors.Length - 1 )
                UniformBlockUncompressedSize = null;
        }

        if( UniformBlockUncompressedSize.HasValue )
            BlockStartPositions = null;
    }

    private protected override BlockData? GetCurrentBlock()
    {
        if( !BlockInfo.HasValue )
        {
            if( StandardBlockDescriptors.Length <= BlockIndex )
                return null;
            BlockInfo = GetBlock( StandardBlockDescriptors.Span[ BlockIndex ].Offset );
        }

        return BlockInfo.Value;
    }

    private protected override (int BlockIndex, int BlockOffset) ResolveBlockPosition( long offset )
    {
        if( UniformBlockUncompressedSize.HasValue )
        {
            var (a, b) = long.DivRem( offset, UniformBlockUncompressedSize.Value );
            return checked( ( (int)a, (int)b ) );
        }

        if( BlockStartPositions == null || BlockStartPositions.Length == 0 )
            throw new InvalidOperationException( "No blocks" );

        // Binary search for the block index
        int idx = BlockStartPositions.AsSpan().BinarySearch( offset );
        if( idx < 0 )
            idx = ~idx - 1;

        return checked( ( idx, (int)( offset - BlockStartPositions[ idx ] ) ) );
    }
}
