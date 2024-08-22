using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lumina.Data.Structs.Excel;

/// <summary>Header of a row that may contain multiple subrows.</summary>
[StructLayout( LayoutKind.Explicit, Size = 6 )]
public struct ExcelDataRowHeader
{
    /// <summary>Size of the row data.</summary>
    [FieldOffset( 0 )] public uint DataSize;

    /// <summary>Number of subrows contained within, if the containing sheet is of <see cref="ExcelVariant.Subrows"/> variant.</summary>
    [FieldOffset( 4 )] public ushort RowCount;

    /// <summary>Creates a new instance of <see cref="ExcelDataRowHeader"/> from a binary-serialized form.</summary>
    /// <param name="reader">Binary reader to read from.</param>
    /// <returns>Read row header.</returns>
    public static ExcelDataRowHeader Read( LuminaBinaryReader reader )
    {
        var buf8 = 0L;
        var buf = MemoryMarshal.Cast< long, byte >( new( ref buf8 ) )[ ..Unsafe.SizeOf< ExcelDataRowHeader >() ];
        buf = buf[ ..reader.Read( buf ) ];
        return FromSpan( buf );
    }

    /// <summary>Creates a new instance of <see cref="ExcelDataRowHeader"/> from a binary-serialized form.</summary>
    /// <param name="data">Binary reader to read from.</param>
    /// <returns>Read row header.</returns>
    public static ExcelDataRowHeader FromSpan( ReadOnlySpan< byte > data ) => new()
    {
        DataSize = BinaryPrimitives.ReadUInt32BigEndian( data ),
        RowCount = BinaryPrimitives.ReadUInt16BigEndian( data ),
    };

    /// <inheritdoc/>
    public override string ToString() => $"{nameof( ExcelDataRowHeader )}({nameof(DataSize)}={DataSize}, {nameof(RowCount)}={RowCount})";
}