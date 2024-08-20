using System.Runtime.InteropServices;
using Lumina.Data;

namespace Lumina.Excel.Rows;

/// <summary>Represents a row or a subrow.</summary>
/// <param name="Page">Page that contains the data for this row.</param>
/// <param name="RowId">ID of the row. This is separate from the row indices.</param>
/// <param name="Offset">Byte offset of the row, relative to the beginning of an exd file.
/// Use <see cref="SubrowOffset"/> if you're accessing subrow data.</param>
/// <param name="Language">Language of the row.</param>
/// <param name="SubrowDataOffset">Offset of the subrow data, or <c>0</c> if the sheet does not support subrows.</param>
/// <param name="SubrowCount">Number of subrows in the row, or <c>1</c> if the sheet does not support subrows.</param>
/// <param name="SubrowId">ID of the subrow in the row, or <c>0</c> if the sheet does not support subrows.</param>
[StructLayout( LayoutKind.Sequential, Size = 24 )]
public readonly record struct RawExcelRow(
    ExcelPage Page,
    uint RowId,
    uint Offset,
    Language Language,
    ushort SubrowDataOffset,
    ushort SubrowCount,
    ushort SubrowId )
{
    /// <summary>Gets the offset of the subrow itself.</summary>
    public uint SubrowOffset => Offset + 2 + SubrowId * ( SubrowDataOffset + 2u );
}