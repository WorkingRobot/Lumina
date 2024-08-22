using System.Runtime.InteropServices;

namespace Lumina.Data.Structs.Excel;

/// <summary>Lookup information for pages w.r.t. a sheet.</summary>
[StructLayout( LayoutKind.Sequential )]
public struct ExcelDataPagination
{
    /// <summary>ID of the first row contained in the page.</summary>
    public uint StartId;

    /// <summary>Number of row IDs claimed by this page. Usually equals to the number of rows, if no gaps exist (page is not sparse.)</summary>
    public uint RowCount;

    /// <summary>Creates a new instance of <see cref="ExcelDataPagination"/> from a binary-serialized form.</summary>
    /// <param name="reader">Binary reader to read from.</param>
    /// <returns>Read pagination data.</returns>
    public static ExcelDataPagination Read( LuminaBinaryReader reader ) => new()
    {
        StartId = reader.ReadUInt32(),
        RowCount = reader.ReadUInt32(),
    };

    /// <inheritdoc/>
    public override string ToString() => $"{nameof( ExcelDataPagination )}({StartId}..{StartId+RowCount})";
}