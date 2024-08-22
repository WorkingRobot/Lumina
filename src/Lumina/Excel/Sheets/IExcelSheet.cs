using System;
using System.Collections.Immutable;
using Lumina.Data;
using Lumina.Data.Structs.Excel;
using Lumina.Excel.Rows;

namespace Lumina.Excel.Sheets;

/// <summary>Represents an Excel sheet.</summary>
/// <remarks>This interface exists for documentation purposes. Actually using this interface is not recommended.</remarks>
internal interface IExcelSheet
{
    /// <summary>The module that this sheet belongs to.</summary>
    ExcelModule Module { get; }

    /// <summary>The language of the rows in this sheet.</summary>
    /// <remarks>This can be different from the requested language if it wasn't supported.</remarks>
    Language Language { get; }

    /// <summary>Gets the name of the sheet.</summary>
    string Name { get; }

    /// <summary>Gets the variant of this sheet.</summary>
    ExcelVariant Variant { get; }

    /// <summary>Contains information on the columns in this sheet.</summary>
    ImmutableArray< ExcelColumnDefinition > Columns { get; }

    /// <summary>Gets the calculated column hash.</summary>
    uint ColumnHash { get; }

    /// <summary>The number of rows in this sheet.</summary>
    /// <remarks>
    /// If this sheet has gaps in row ids, it returns the number of rows that exist, not the highest row id.
    /// If this sheet has subrows, this will still return the number of rows and not the total number of subrows.
    /// </remarks>
    int Count { get; }

    /// <summary>Gets the raw rows.</summary>
    ReadOnlySpan< RawExcelRow > RawRows { get; }

    /// <summary>Whether this sheet has a row with the given <paramref name="rowId"/>.</summary>
    /// <remarks>If this sheet has subrows, this will check if the row id has any subrows.</remarks>
    /// <param name="rowId">The row id to check.</param>
    /// <returns>Whether the row exists.</returns>
    bool HasRow( uint rowId );
}