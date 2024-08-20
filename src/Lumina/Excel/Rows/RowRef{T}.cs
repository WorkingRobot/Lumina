using System;
using Lumina.Excel.Sheets;

namespace Lumina.Excel.Rows;

/// <summary>
/// A helper type to concretely reference a row in a specific Excel sheet.
/// </summary>
/// <typeparam name="T">The row type referenced by the <see cref="RowId"/>.</typeparam>
/// <param name="module">The <see cref="ExcelModule"/> to read sheet data from.</param>
/// <param name="rowId">The referenced row id.</param>
public readonly struct RowRef< T >( ExcelModule? module, uint rowId ) where T : IExcelRow< T >
{
    private readonly ExcelSheet< T >? _sheet = module?.GetSheet< T >();

    /// <summary>Gets the ID of the referenced row.</summary>
    public uint RowId => rowId;

    /// <summary>Gets a value indicating whether the <see cref="RowId"/> exists in the sheet.</summary>
    public bool IsValid => _sheet?.HasRow( RowId ) ?? false;

    /// <summary>Gets the referenced row.</summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IsValid"/> is false.</exception>
    public T Value => ValueOrDefault ?? throw new InvalidOperationException();

    /// <summary>Gets the referenced row, if possible.</summary>
    /// <value>Instance of <typeparamref name="T"/> if corresponding row could be found; <see langword="default"/> otherwise.</value>
    /// <remarks>In case <typeparamref name="T"/> is a value type (<see langword="struct"/>), you can use <see cref="ExcelRowExtensions.AsNullable{T}"/> to
    /// convert it to a <see cref="Nullable{T}"/>-wrapped type.</remarks>
    public T? ValueOrDefault => _sheet is null ? default : _sheet.Value.GetRowOrDefault( rowId );

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(RowRef<T>)}({typeof( T ).Name}#{rowId})";

    private RowRef ToGeneric() => RowRef.Create< T >( module, rowId );

    /// <summary>
    /// Converts a concrete <see cref="RowRef{T}"/> to a generic and dynamically typed <see cref="RowRef"/>.
    /// </summary>
    /// <param name="row">The <see cref="RowRef{T}"/> to convert.</param>
    public static explicit operator RowRef( RowRef< T > row ) => row.ToGeneric();
}