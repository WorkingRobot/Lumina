using System;
using Lumina.Excel.Sheets;

namespace Lumina.Excel.Rows;

/// <summary>
/// A helper type to concretely reference a collection of subrows in a specific Excel sheet.
/// </summary>
/// <typeparam name="T">Type of the row referenced by the <see cref="RowId"/>.</typeparam>
/// <param name="Module"><see cref="ExcelModule"/> to read sheet data from.</param>
/// <param name="RowId">ID of the referenced row.</param>
public readonly record struct SubrowRef< T >( ExcelModule? Module, uint RowId ) where T : IExcelRow< T >
{
    private readonly SubrowExcelSheet< T >? _sheet = Module?.GetSubrowSheet< T >();

    /// <summary>Gets a value indicating whether the <see cref="RowId"/> exists in the sheet.</summary>
    public bool IsValid => _sheet?.HasRow( RowId ) ?? false;

    /// <summary>Gets the referenced row as a subrow collection.</summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IsValid"/> is false.</exception>
    public SubrowExcelSheet< T >.SubrowCollection Value => ValueOrDefault ?? throw new InvalidOperationException();

    /// <summary>Gets the referenced row as a subrow collection, if possible.</summary>
    /// <value>Instance of <typeparamref name="T"/> if corresponding row could be found; <see langword="default"/> otherwise.</value>
    public SubrowExcelSheet< T >.SubrowCollection? ValueOrDefault => _sheet?.GetRowOrDefault( RowId );

    /// <inheritdoc/>
    public override string ToString() => $"{nameof( SubrowRef< T > )}({typeof( T ).Name}#{RowId})";

    private RowRef ToGeneric() => RowRef.CreateSubrow< T >( Module, RowId );

    /// <summary>
    /// Converts a concrete <see cref="SubrowRef{T}"/> to a generic and dynamically typed <see cref="RowRef"/>.
    /// </summary>
    /// <param name="row">The <see cref="SubrowRef{T}"/> to convert.</param>
    public static explicit operator RowRef( SubrowRef< T > row ) => row.ToGeneric();
}