using System;
using Lumina.Excel.Sheets;

namespace Lumina.Excel.Rows;

/// <summary>
/// A helper type to concretely reference a collection of subrows in a specific Excel sheet.
/// </summary>
/// <typeparam name="T">The subrow type referenced by the subrows of <see cref="RowId"/>.</typeparam>
/// <param name="module">The <see cref="ExcelModule"/> to read sheet data from.</param>
/// <param name="rowId">The referenced row id.</param>
public readonly struct SubrowRef< T >( ExcelModule? module, uint rowId ) where T : IExcelRow< T >
{
    private readonly SubrowExcelSheet< T >? _sheet = module?.GetSubrowSheet< T >();

    /// <summary>Gets the ID of the referenced row.</summary>
    public uint RowId => rowId;

    /// <summary>Gets a value indicating whether the <see cref="RowId"/> exists in the sheet.</summary>
    public bool IsValid => _sheet?.HasRow( RowId ) ?? false;

    /// <summary>Gets the referenced row as a subrow collection.</summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IsValid"/> is false.</exception>
    public SubrowExcelSheet< T >.SubrowCollection Value => ValueOrDefault ?? throw new InvalidOperationException();

    /// <summary>Gets the referenced row as a subrow collection, if possible.</summary>
    /// <value>Instance of <typeparamref name="T"/> if corresponding row could be found; <see langword="default"/> otherwise.</value>
    public SubrowExcelSheet< T >.SubrowCollection? ValueOrDefault => _sheet?.GetRowOrDefault( rowId );

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(SubrowRef<T>)}({typeof( T ).Name}#{rowId})";

    private RowRef ToGeneric() => RowRef.CreateSubrow< T >( module, rowId );

    /// <summary>
    /// Converts a concrete <see cref="SubrowRef{T}"/> to a generic and dynamically typed <see cref="RowRef"/>.
    /// </summary>
    /// <param name="row">The <see cref="SubrowRef{T}"/> to convert.</param>
    public static explicit operator RowRef( SubrowRef< T > row ) => row.ToGeneric();
}