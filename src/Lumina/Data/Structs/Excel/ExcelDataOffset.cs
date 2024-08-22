using System.Runtime.InteropServices;

namespace Lumina.Data.Structs.Excel;

/// <summary>Lookup information for row data.</summary>
[StructLayout( LayoutKind.Sequential )]
public struct ExcelDataOffset
{
    /// <summary>ID of the row.</summary>
    public uint RowId;

    /// <summary>Offset of the data, w.r.t. the beginning of a .exd file.</summary>
    public uint Offset;

    /// <summary>Creates a new instance of <see cref="ExcelDataOffset"/> from a binary-serialized form.</summary>
    /// <param name="reader">Binary reader to read from.</param>
    /// <returns>Read row offset data.</returns>
    public static ExcelDataOffset Read( LuminaBinaryReader reader ) => new()
    {
        RowId = reader.ReadUInt32(),
        Offset = reader.ReadUInt32(),
    };

    /// <inheritdoc/>
    public override string ToString() => $"{nameof( ExcelDataOffset )}#{RowId} at {Offset}";
}