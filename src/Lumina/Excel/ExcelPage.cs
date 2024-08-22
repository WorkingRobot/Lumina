using Lumina.Extensions;
using Lumina.Text.ReadOnly;
using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lumina.Excel.Sheets;

namespace Lumina.Excel;

/// <summary>
/// A single page/file of data from an Excel sheet.
/// </summary>
/// <remarks>
/// If you only want to read Excel sheets, please refrain from touching this class. This class exists mostly as an implementation detail for parsing Excel rows.
/// </remarks>
[EditorBrowsable( EditorBrowsableState.Advanced )]
public sealed class ExcelPage
{
    private readonly byte[] _data;
    private readonly int _pageIndex;
    private readonly ushort _dataOffset;

    internal ExcelPage( RawExcelSheet rawSheet, int pageIndex, byte[] pageData, ushort headerDataOffset )
    {
        RawSheet = rawSheet;
        _pageIndex = pageIndex;
        _data = pageData;
        _dataOffset = headerDataOffset;
    }

    /// <summary>Gets the module that this page belongs to.</summary>
    public ExcelModule Module => RawSheet.Module;

    /// <summary>Gets the raw sheet that this page belongs to.</summary>
    public RawExcelSheet RawSheet { get; }

    private ReadOnlyMemory< byte > Data => _data;

    // Ignores bounds checks to speed up reading data.
    // https://t.ly/EmR4n (Sharplab link)
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private D Read< D >( nuint offset ) where D : unmanaged =>
        Unsafe.As< byte, D >( ref Unsafe.AddByteOffset( ref MemoryMarshal.GetArrayDataReference( _data ), offset ) );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static float ReverseEndianness( float v ) =>
        Unsafe.BitCast< uint, float >( BinaryPrimitives.ReverseEndianness( Unsafe.BitCast< float, uint >( v ) ) );

    /// <summary>
    /// Reads a <see cref="ReadOnlySeString"/> from the page data at <paramref name="offset"/>.
    /// </summary>
    /// <remarks>
    /// If the string is a valid RSV string and <see cref="LuminaOptions.RsvResolver"/> is set, the resolved string will be returned if it exists.
    /// </remarks>
    /// <param name="offset">Offset of the field inside the page.</param>
    /// <param name="structOffset">Offset of the row inside the page.</param>
    /// <returns>The <see cref="ReadOnlySeString"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public ReadOnlySeString ReadString( nuint offset, nuint structOffset )
    {
        offset = ReadUInt32( offset ) + structOffset + _dataOffset;
        var data = Data[ (int) offset.. ];
        var stringLength = data.Span.IndexOf( (byte) 0 );
        var ret = new ReadOnlySeString( data[ ..stringLength ] );
        if( ret.IsRsv() && Module.RsvResolver != null )
        {
            if( Module.RsvResolver.Invoke( ret, out var resolvedString ) )
                return resolvedString;
        }

        return ret;
    }

    /// <summary>
    /// Reads a <see cref="bool"/> from the page data at <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">Offset of the field inside the page.</param>
    /// <returns>The <see cref="bool"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public bool ReadBool( nuint offset ) =>
        Read< bool >( offset );

    /// <summary>
    /// Reads a <see cref="sbyte"/> from the page data at <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">Offset of the field inside the page.</param>
    /// <returns>The <see cref="sbyte"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public sbyte ReadInt8( nuint offset ) =>
        Read< sbyte >( offset );

    /// <summary>
    /// Reads a <see cref="byte"/> from the page data at <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">Offset of the field inside the page.</param>
    /// <returns>The <see cref="byte"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public byte ReadUInt8( nuint offset ) =>
        Read< byte >( offset );

    /// <summary>
    /// Reads a <see cref="short"/> from the page data at <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">Offset of the field inside the page.</param>
    /// <returns>The <see cref="short"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public short ReadInt16( nuint offset ) =>
        BinaryPrimitives.ReverseEndianness( Read< short >( offset ) );

    /// <summary>
    /// Reads a <see cref="ushort"/> from the page data at <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">Offset of the field inside the page.</param>
    /// <returns>The <see cref="ushort"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public ushort ReadUInt16( nuint offset ) =>
        BinaryPrimitives.ReverseEndianness( Read< ushort >( offset ) );

    /// <summary>
    /// Reads a <see cref="int"/> from the page data at <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">Offset of the field inside the page.</param>
    /// <returns>The <see cref="int"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public int ReadInt32( nuint offset ) =>
        BinaryPrimitives.ReverseEndianness( Read< int >( offset ) );

    /// <summary>
    /// Reads a <see cref="uint"/> from the page data at <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">Offset of the field inside the page.</param>
    /// <returns>The <see cref="uint"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public uint ReadUInt32( nuint offset ) =>
        BinaryPrimitives.ReverseEndianness( Read< uint >( offset ) );

    /// <summary>
    /// Reads a <see cref="float"/> from the page data at <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">Offset of the field inside the page.</param>
    /// <returns>The <see cref="float"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public float ReadFloat32( nuint offset ) =>
        ReverseEndianness( Read< float >( offset ) );

    /// <summary>
    /// Reads a <see cref="long"/> from the page data at <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">Offset of the field inside the page.</param>
    /// <returns>The <see cref="long"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public long ReadInt64( nuint offset ) =>
        BinaryPrimitives.ReverseEndianness( Read< long >( offset ) );

    /// <summary>
    /// Reads a <see cref="ulong"/> from the page data at <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">Offset of the field inside the page.</param>
    /// <returns>The <see cref="ulong"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public ulong ReadUInt64( nuint offset ) =>
        BinaryPrimitives.ReverseEndianness( Read< ulong >( offset ) );

    /// <summary>
    /// Reads a <see cref="bool"/> from the page data at <paramref name="offset"/> at bit offset <paramref name="bit"/>.
    /// </summary>
    /// <param name="offset">Byte offset of the field inside the page.</param>
    /// <param name="bit">Bit offset of the field inside the byte. (0 - 7)</param>
    /// <returns>The <see cref="bool"/>.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public bool ReadPackedBool( nuint offset, byte bit ) =>
        ( Read< byte >( offset ) & ( 1 << bit ) ) != 0;

    /// <inheritdoc/>
    public override string ToString() => $"{nameof( ExcelPage )}({RawSheet} page {_pageIndex})";
}