using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Lumina.Data;
using Lumina.Data.Structs.Excel;
using Lumina.Excel.Exceptions;
using Lumina.Excel.Rows;

namespace Lumina.Excel.Sheets;

/// <summary>A typed Excel sheet of <see cref="ExcelVariant.Default"/> variant that wraps around a <see cref="RawExcelSheet"/>.</summary>
/// <typeparam name="T">Type of the rows contained within.</typeparam>
public readonly partial struct ExcelSheet< T >
    : IExcelSheet
        , ICollection< T >
        , IReadOnlyCollection< T >
        , IEquatable< ExcelSheet< T > >
    where T : IExcelRow< T >
{
    private readonly object?[]? _rowCache;

    /// <summary>Creates a new instance of <see cref="ExcelSheet{T}"/>, deducing column hash from <see cref="SheetAttribute"/> of <see cref="T"/> if available.
    /// </summary>
    /// <param name="rawSheet">Raw sheet to base this sheet on.</param>
    /// <param name="rowCache">Row cache to use, if <typeparamref name="T"/> is a reference type.</param>
    /// <exception cref="MismatchedColumnHashException">Column hash deduced from sheet attribute was invalid (hash mismatch).</exception>
    /// <exception cref="NotSupportedException">Header file had a <see cref="ExcelVariant"/> value that is not supported.</exception>
    /// <returns>A new instance of <see cref="ExcelSheet{T}"/>.</returns>
    public ExcelSheet( RawExcelSheet rawSheet, object?[]? rowCache = null )
        : this( rawSheet, rawSheet.Module.GetSheetAttributes< T >()?.ColumnHash, rowCache )
    { }

    /// <summary>Creates a new instance of <see cref="ExcelSheet{T}"/>.</summary>
    /// <param name="rawSheet">Raw sheet to base this sheet on.</param>
    /// <param name="columnHash">Hash of the columns in the sheet. If <see langword="null"/>, it will not check the hash.</param>
    /// <param name="rowCache">Row cache to use, if <typeparamref name="T"/> is a reference type.</param>
    /// <exception cref="MismatchedColumnHashException"><paramref name="columnHash"/> was invalid (hash mismatch).</exception>
    /// <exception cref="NotSupportedException">Header file had a <see cref="ExcelVariant"/> value that is not supported.</exception>
    /// <returns>A new instance of <see cref="ExcelSheet{T}"/>.</returns>
    public ExcelSheet( RawExcelSheet rawSheet, uint? columnHash, object?[]? rowCache = null )
    {
        if( typeof( T ).IsValueType )
            rowCache = null;

        if( rawSheet.Variant != ExcelVariant.Default )
            throw new NotSupportedException( $"Sheet is not of {nameof( ExcelVariant.Default )} variant." );
        if( columnHash is not null && columnHash.Value != rawSheet.ColumnHash )
            throw new MismatchedColumnHashException( rawSheet.ColumnHash, columnHash.Value, nameof( columnHash ) );
        if( rowCache is not null && rowCache.Length < rawSheet.Count )
            throw new ArgumentException( "Size of cache must be at least the number of rows in the sheet.", nameof( rowCache ) );

        RawSheet = rawSheet;
        _rowCache = rowCache;
    }

    /// <summary>Gets the raw sheet this typed sheet is based on.</summary>
    public RawExcelSheet RawSheet { get; }

    /// <inheritdoc/>
    public ExcelModule Module => RawSheet.Module;

    /// <inheritdoc/>
    public Language Language => RawSheet.Language;

    /// <inheritdoc/>
    public string Name => RawSheet.Name;

    /// <inheritdoc/>
    public ExcelVariant Variant => RawSheet.Variant;

    /// <inheritdoc/>
    public IReadOnlyList< ExcelColumnDefinition > Columns => RawSheet.Columns;

    /// <inheritdoc/>
    public uint ColumnHash => RawSheet.ColumnHash;

    /// <inheritdoc cref="IExcelSheet.Count"/>
    public int Count => RawSheet.Count;

    /// <inheritdoc/>
    public ReadOnlySpan< RawExcelRow > RawRows => RawSheet.RawRows;

    bool ICollection< T >.IsReadOnly => true;

    /// <inheritdoc cref="GetRow"/>
    public T this[ uint rowId ] => GetRow( rowId );

    /// <inheritdoc/>
    public ushort GetColumnOffset( int columnIdx ) => RawSheet.GetColumnOffset( columnIdx );

    /// <inheritdoc/>
    public bool HasRow( uint rowId ) => RawSheet.HasRow( rowId );

    /// <summary>
    /// Tries to get the <paramref name="rowId"/>th row in this sheet.
    /// </summary>
    /// <param name="rowId">The row id to get.</param>
    /// <returns>A nullable row object. Returns <see langword="null"/> if the row does not exist.</returns>
    /// <remarks>In case <typeparamref name="T"/> is a value type (<see langword="struct"/>), you can use <see cref="ExcelRowExtensions.AsNullable{T}"/> to
    /// convert it to a <see cref="Nullable{T}"/>-wrapped type.</remarks>
    public T? GetRowOrDefault( uint rowId )
    {
        ref readonly var lookup = ref RawSheet.GetRawRowOrNullRef( rowId, out var rowIndex );
        return Unsafe.IsNullRef( in lookup ) ? default : UnsafeCreateRow( rowIndex, in lookup );
    }

    /// <summary>
    /// Tries to get the <paramref name="rowId"/>th row in this sheet.
    /// </summary>
    /// <param name="rowId">The row id to get.</param>
    /// <param name="row">The output row object.</param>
    /// <returns><see langword="true"/> if the row exists and <paramref name="row"/> is written to and <see langword="false"/> otherwise.</returns>
    public bool TryGetRow( uint rowId, [MaybeNullWhen( false )] out T row )
    {
        ref readonly var lookup = ref RawSheet.GetRawRowOrNullRef( rowId, out var rowIndex );
        if( Unsafe.IsNullRef( in lookup ) )
        {
            row = default;
            return false;
        }

        row = UnsafeCreateRow( rowIndex, in lookup );
        return true;
    }

    /// <summary>
    /// Gets the <paramref name="rowId"/>th row in this sheet.
    /// </summary>
    /// <param name="rowId">The row id to get.</param>
    /// <returns>A row object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws when the row id does not have a row attached to it.</exception>
    public T GetRow( uint rowId )
    {
        ref readonly var lookup = ref RawSheet.GetRawRowOrNullRef( rowId, out var rowIndex );
        return Unsafe.IsNullRef( in lookup ) ? throw new ArgumentOutOfRangeException( nameof( rowId ), rowId, null ) : UnsafeCreateRow( rowIndex, in lookup );
    }

    /// <summary>
    /// Gets the <paramref name="rowIndex"/>th row in this sheet, ordered by row id in ascending order.
    /// </summary>
    /// <remarks>If you are looking to find a row by its id, use <see cref="GetRow(uint)"/> instead.</remarks>
    /// <param name="rowIndex">The zero-based index of this row.</param>
    /// <returns>A row object.</returns>
    public T GetRowAt( int rowIndex )
    {
        ArgumentOutOfRangeException.ThrowIfNegative( rowIndex );
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( rowIndex, RawRows.Length );

        return UnsafeCreateRowAt( rowIndex );
    }

    /// <inheritdoc/>
    public bool Contains( T item ) => TryGetRow( item.RawRow.RowId, out var row ) && EqualityComparer< T >.Default.Equals( item, row );

    /// <inheritdoc/>
    public void CopyTo( T[] array, int arrayIndex )
    {
        ArgumentNullException.ThrowIfNull( array );
        ArgumentOutOfRangeException.ThrowIfNegative( arrayIndex );
        if( Count > array.Length - arrayIndex )
            throw new ArgumentException( "The number of elements in the source list is greater than the available space." );

        var rowIndex = 0;
        foreach( var lookup in RawRows )
            array[ arrayIndex++ ] = UnsafeCreateRow( rowIndex++, in lookup );
    }

    void ICollection< T >.Add( T item ) => throw new NotSupportedException();

    void ICollection< T >.Clear() => throw new NotSupportedException();

    bool ICollection< T >.Remove( T item ) => throw new NotSupportedException();

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new( this );

    IEnumerator< T > IEnumerable< T >.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public override string ToString() => $"{Name}<{typeof( T ).Name}>({Language}, {Variant}, {Count} row(s), {Columns.Count} column(s))";

    /// <inheritdoc/>
    public bool Equals(ExcelSheet< T > other) => RawSheet.Equals(other.RawSheet);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ExcelSheet< T > other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine( RawSheet, typeof( T ) );

    /// <summary>Compares two values to determine equality.</summary>
    /// <param name="left">The value to compare with <paramref name="right" />.</param>
    /// <param name="right">The value to compare with <paramref name="left" />.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is equal to <paramref name="right" />; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(ExcelSheet< T > left, ExcelSheet< T > right) => left.Equals(right);

    /// <summary>Compares two values to determine inequality.</summary>
    /// <param name="left">The value to compare with <paramref name="right" />.</param>
    /// <param name="right">The value to compare with <paramref name="left" />.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is not equal to <paramref name="right" />; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(ExcelSheet< T > left, ExcelSheet< T > right) => !left.Equals(right);

    /// <summary>Creates a row at the given index, without checking for bounds or preconditions.</summary>
    /// <param name="rowIndex">Index of the desired row.</param>
    /// <returns>A new instance of <typeparamref name="T"/>.</returns>
    private T UnsafeCreateRowAt( int rowIndex ) => UnsafeCreateRow( rowIndex, in RawSheet.UnsafeGetRowLookupAt( rowIndex ) );

    /// <summary>Creates a row using the given lookup data, without checking for bounds or preconditions.</summary>
    /// <param name="rowIndex">Index of the desired row.</param>
    /// <param name="row">Lookup data for the desired row.</param>
    /// <returns>A new instance of <typeparamref name="T"/>.</returns>
    private T UnsafeCreateRow( int rowIndex, scoped ref readonly RawExcelRow row )
    {
        if( _rowCache is null )
            return T.Create( row );

        ref var slot = ref Unsafe.Add( ref MemoryMarshal.GetArrayDataReference( _rowCache ), rowIndex );
        if( slot is null )
            Interlocked.CompareExchange( ref slot, T.Create( row ), null );

        return (T) slot;
    }
}