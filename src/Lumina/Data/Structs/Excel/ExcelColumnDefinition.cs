using System.Runtime.InteropServices;

namespace Lumina.Data.Structs.Excel;

/// <summary>Lookup information for column data w.r.t. a row.</summary>
[StructLayout( LayoutKind.Sequential )]
public struct ExcelColumnDefinition
{
    /// <summary>Type of the data contained in the column.</summary>
    public ExcelColumnDataType Type;

    /// <summary>Offset of the data w.r.t. a row.</summary>
    public ushort Offset;

    /// <summary>Gets a value indicating whether this column contains a boolean value.</summary>
    public readonly bool IsBoolType =>
        Type is ExcelColumnDataType.Bool
            or ExcelColumnDataType.PackedBool0
            or ExcelColumnDataType.PackedBool1
            or ExcelColumnDataType.PackedBool2
            or ExcelColumnDataType.PackedBool3
            or ExcelColumnDataType.PackedBool4
            or ExcelColumnDataType.PackedBool5
            or ExcelColumnDataType.PackedBool6
            or ExcelColumnDataType.PackedBool7;

    /// <summary>Creates a new instance of <see cref="ExcelColumnDefinition"/> from a binary-serialized form.</summary>
    /// <param name="reader">Binary reader to read from.</param>
    /// <returns>Read column data.</returns>
    public static ExcelColumnDefinition Read( LuminaBinaryReader reader ) => new()
    {
        Type = (ExcelColumnDataType) reader.ReadUInt16(),
        Offset = reader.ReadUInt16(),
    };

    /// <inheritdoc/>
    public override string ToString() => $"{nameof( ExcelColumnDefinition )}@{Offset}: {Type}";
}