using System;
using System.Runtime.InteropServices;
using Lumina.Data.Files.Excel;
using Lumina.Excel.Rows;

namespace Lumina.Data.Structs.Excel;

/// <summary>Header of a <see cref="ExcelHeaderFile"/>.</summary>
[StructLayout( LayoutKind.Sequential, Size = 32 )]
public struct ExcelHeaderHeader
{
    /// <summary>Expected value for <see cref="Magic"/>.</summary>
    public const uint ExpectedMagic = 0x45584846; // 'EXHF'

    /// <summary>File magic value.</summary>
    public uint Magic;

    /// <summary>Version of the file.</summary>
    // todo: not sure? maybe?
    public ushort Version;

    /// <summary>Offset to subrow data (=byte length of fixed-layout data shared by all rows).</summary>
    /// <remarks>See <see cref="RawExcelRow.SubrowOffset"/> for offset calculation.</remarks>
    public ushort DataOffset;

    /// <summary>Number of columns contained in the sheet.</summary>
    public ushort ColumnCount;

    /// <summary>Number of pages contained in the sheet.</summary>
    public ushort PageCount;

    /// <summary>Number of languages contained in the sheet.</summary>
    public ushort LanguageCount;

    /// <summary>Packed value dictating how to manage rows of a sheet.</summary>
    private ushort LoadMethodAndCountPerChunk;

    /// <summary>Type of the sheet.</summary>
    public ExcelVariant Variant;

    /// <summary>Unknown value 3.</summary>
    private ushort _unknown1;

    /// <summary>Number of rows in the sheet, not counting the gaps, if any.</summary>
    public uint RowCount;

    /// <summary>Unknown value 4.</summary>
    private ulong _unknown2;

    /// <summary>Gets or sets the method the game will use to manage the rows.</summary>
    public ExcelMemoryLoadMethod LoadMethod {
        readonly get => (ExcelMemoryLoadMethod) ( LoadMethodAndCountPerChunk >> 14 );
        set {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( LoadMethodAndCountPerChunk, 4 );
            LoadMethodAndCountPerChunk = (ushort) ( ( LoadMethodAndCountPerChunk & 0x3FFF ) | ( (ushort) value << 14 ) );
        }
    }

    /// <summary>Gets or sets the number of items to keep in memory at a time, if <see cref="ExcelMemoryLoadMethod.RingBuffer"/> is used.</summary>
    public ushort CountPerChunk {
        readonly get => (ushort) ( LoadMethodAndCountPerChunk & 0x3FFF );
        set {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( LoadMethodAndCountPerChunk, 0x4000 );
            LoadMethodAndCountPerChunk = (ushort) ( ( LoadMethodAndCountPerChunk & 0xC000 ) | value );
        }
    }

    /// <summary>Creates a new instance of <see cref="ExcelHeaderHeader"/> from a binary-serialized form.</summary>
    /// <param name="reader">Binary reader to read from.</param>
    /// <returns>Read header data.</returns>
    public static ExcelHeaderHeader Read( LuminaBinaryReader reader ) => new()
    {
        Magic = reader.ReadUInt32(),
        Version = reader.ReadUInt16(),
        DataOffset = reader.ReadUInt16(),
        ColumnCount = reader.ReadUInt16(),
        PageCount = reader.ReadUInt16(),
        LanguageCount = reader.ReadUInt16(),
        LoadMethodAndCountPerChunk = reader.ReadUInt16(),
        Variant = (ExcelVariant) reader.ReadUInt16(),
        _unknown1 = reader.ReadUInt16(),
        RowCount = reader.ReadUInt32(),
        _unknown2 = reader.ReadUInt64(),
    };

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof( ExcelHeaderHeader )}(v{Version}, {Variant}, {ColumnCount} column(s), {PageCount} page(s), {LanguageCount} language(s), {nameof(DataOffset)}={DataOffset}, {nameof(LoadMethod)}={LoadMethod})";
}