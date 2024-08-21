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

/// <summary>A typed Excel sheet of <see cref="ExcelVariant.Subrows"/> variant that wraps around a <see cref="RawSubrowExcelSheet"/>.</summary>
/// <typeparam name="T">Type of the rows contained within.</typeparam>
public readonly partial struct SubrowExcelSheet< T >
    : ISubrowExcelSheet, ICollection< SubrowExcelSheet< T >.SubrowCollection >, IReadOnlyCollection< SubrowExcelSheet< T >.SubrowCollection >
    where T : IExcelRow< T >
{
    private readonly object?[]?[]? _rowCache;

    /// <summary>Creates a new instance of <see cref="SubrowExcelSheet{T}"/>, deducing column hash from <see cref="SheetAttribute"/> of <see cref="T"/> if available.
    /// </summary>
    /// <param name="rawSheet">Raw sheet to base this sheet on.</param>
    /// <param name="rowCache">Row cache to use, if <typeparamref name="T"/> is a reference type.</param>
    /// <exception cref="MismatchedColumnHashException">Column hash deduced from sheet attribute was invalid (hash mismatch).</exception>
    /// <exception cref="NotSupportedException">Header file had a <see cref="ExcelVariant"/> value that is not supported.</exception>
    /// <returns>A new instance of <see cref="ExcelSheet{T}"/>.</returns>
    public SubrowExcelSheet( RawSubrowExcelSheet rawSheet, object?[]?[]? rowCache = null )
        : this( rawSheet, rawSheet.Module.GetSheetAttributes< T >()?.ColumnHash, rowCache )
    { }

    /// <summary>Creates a new instance of <see cref="SubrowExcelSheet{T}"/>.</summary>
    /// <param name="rawSheet">Raw sheet to base this sheet on.</param>
    /// <param name="columnHash">Hash of the columns in the sheet. If <see langword="null"/>, it will not check the hash.</param>
    /// <param name="rowCache">Row cache to use, if <typeparamref name="T"/> is a reference type.</param>
    /// <exception cref="MismatchedColumnHashException"><paramref name="columnHash"/> was invalid (hash mismatch).</exception>
    /// <exception cref="NotSupportedException">Header file had a <see cref="ExcelVariant"/> value that is not supported.</exception>
    /// <returns>A new instance of <see cref="ExcelSheet{T}"/>.</returns>
    public SubrowExcelSheet( RawSubrowExcelSheet rawSheet, uint? columnHash, object?[]?[]? rowCache = null )
    {
        if( typeof( T ).IsValueType )
            rowCache = null;

        if( rawSheet.Variant != ExcelVariant.Subrows )
            throw new NotSupportedException( $"Sheet is not of {nameof( ExcelVariant.Subrows )} variant." );
        if( columnHash is not null && columnHash.Value != rawSheet.ColumnHash )
            throw new MismatchedColumnHashException( rawSheet.ColumnHash, columnHash.Value, nameof( columnHash ) );
        if( rowCache is not null && rowCache.Length < rawSheet.Count )
            throw new ArgumentException( "Size of cache must be at least the number of rows in the sheet.", nameof( rowCache ) );

        RawSheet = rawSheet;
        _rowCache = rowCache;
    }

    /// <summary>Gets the raw sheet this typed sheet is based on.</summary>
    public RawSubrowExcelSheet RawSheet { get; }

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

    /// <inheritdoc/>
    public int TotalSubrowCount => RawSheet.TotalSubrowCount;

    /// <inheritdoc/>
    public ushort GetColumnOffset( int columnIdx ) => RawSheet.GetColumnOffset( columnIdx );

    /// <inheritdoc/>
    public bool HasRow( uint rowId ) => RawSheet.HasRow( rowId );

    /// <inheritdoc/>
    public bool HasSubrow( uint rowId, ushort subrowId ) => RawSheet.HasSubrow( rowId, subrowId );

    /// <inheritdoc/>
    public bool TryGetSubrowCount( uint rowId, out ushort subrowCount ) => RawSheet.TryGetSubrowCount( rowId, out subrowCount );

    /// <inheritdoc/>
    public ushort GetSubrowCount( uint rowId ) => RawSheet.GetSubrowCount( rowId );

    /// <inheritdoc/>
    bool ICollection< SubrowCollection >.IsReadOnly => true;

    /// <inheritdoc cref="GetRow"/>
    public SubrowCollection this[ uint rowId ] => GetRow( rowId );

    /// <inheritdoc cref="GetSubrow"/>
    public T this[ uint rowId, ushort subrowId ] => GetSubrow( rowId, subrowId );

    /// <summary>
    /// Tries to get the subrow collection with row id <paramref name="rowId"/> in this sheet.
    /// </summary>
    /// <param name="rowId">The row id to get.</param>
    /// <returns>A nullable subrow collection object. Returns <see langword="null"/> if the row does not exist.</returns>
    public SubrowCollection? GetRowOrDefault( uint rowId )
    {
        ref readonly var lookup = ref RawSheet.GetRawRowOrNullRef( rowId, out var rowIndex );
        return Unsafe.IsNullRef( in lookup ) ? null : new( this, in lookup, rowIndex );
    }

    /// <summary>
    /// Tries to get the subrow collection with row id <paramref name="rowId"/> in this sheet.
    /// </summary>
    /// <param name="rowId">The row id to get.</param>
    /// <param name="row">The output subrow collection object.</param>
    /// <returns><see langword="true"/> if the row exists and <paramref name="row"/> is written to and <see langword="false"/> otherwise.</returns>
    public bool TryGetRow( uint rowId, out SubrowCollection row )
    {
        ref readonly var lookup = ref RawSheet.GetRawRowOrNullRef( rowId, out var rowIndex );
        if( Unsafe.IsNullRef( in lookup ) )
        {
            row = default;
            return false;
        }

        row = new( this, in lookup, rowIndex );
        return true;
    }

    /// <summary>
    /// Gets the subrow collection with row id <paramref name="rowId"/> in this sheet. Throws if the row does not exist.
    /// </summary>
    /// <param name="rowId">The row id to get.</param>
    /// <returns>A subrow collection object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the sheet does not have a row at that <paramref name="rowId"/>.</exception>
    public SubrowCollection GetRow( uint rowId )
    {
        ref readonly var lookup = ref RawSheet.GetRawRowOrNullRef( rowId, out var rowIndex );
        return Unsafe.IsNullRef( in lookup ) ? throw new ArgumentOutOfRangeException( nameof( rowId ), rowId, null ) : new( this, in lookup, rowIndex );
    }

    /// <summary>
    /// Gets the subrow collection of the <paramref name="rowIndex"/>th row in this sheet, ordered by row id in ascending order.
    /// </summary>
    /// <remarks>If you are looking to find a row by its id, use <see cref="GetRow(uint)"/> instead.</remarks>
    /// <param name="rowIndex">The zero-based index of this row.</param>
    /// <returns>A subrow collection object.</returns>
    public SubrowCollection GetRowAt( int rowIndex )
    {
        ArgumentOutOfRangeException.ThrowIfNegative( rowIndex );
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( rowIndex, RawRows.Length );

        return new( this, in RawSheet.UnsafeGetRowLookupAt( rowIndex ), rowIndex );
    }

    /// <summary>
    /// Tries to get the <paramref name="subrowId"/>th subrow with row id <paramref name="rowId"/> in this sheet.
    /// </summary>
    /// <param name="rowId">The row id to get.</param>
    /// <param name="subrowId">The subrow id to get.</param>
    /// <returns>A nullable row object. Returns null if the subrow does not exist.</returns>
    /// <remarks>In case <typeparamref name="T"/> is a value type (<see langword="struct"/>), you can use <see cref="ExcelRowExtensions.AsNullable{T}"/> to
    /// convert it to a <see cref="Nullable{T}"/>-wrapped type.</remarks>
    public T? GetSubrowOrDefault( uint rowId, ushort subrowId )
    {
        ref readonly var lookup = ref RawSheet.GetRawRowOrNullRef( rowId, out var rowIndex );
        return Unsafe.IsNullRef( in lookup ) || subrowId >= lookup.SubrowCount ? default : UnsafeCreateSubrow( rowIndex, subrowId, in lookup );
    }

    /// <summary>
    /// Tries to get the <paramref name="subrowId"/>th subrow with row id <paramref name="rowId"/> in this sheet.
    /// </summary>
    /// <param name="rowId">The row id to get.</param>
    /// <param name="subrowId">The subrow id to get.</param>
    /// <param name="subrow">The output row object.</param>
    /// <returns><see langword="true"/> if the subrow exists and <paramref name="subrow"/> is written to and <see langword="false"/> otherwise.</returns>
    public bool TryGetSubrow( uint rowId, ushort subrowId, [MaybeNullWhen( false )] out T subrow )
    {
        ref readonly var lookup = ref RawSheet.GetRawRowOrNullRef( rowId, out var rowIndex );
        if( Unsafe.IsNullRef( in lookup ) || subrowId >= lookup.SubrowCount )
        {
            subrow = default;
            return false;
        }

        subrow = UnsafeCreateSubrow( rowIndex, subrowId, in lookup );
        return true;
    }

    /// <summary>
    /// Gets the <paramref name="subrowId"/>th subrow with row id <paramref name="rowId"/> in this sheet. Throws if the subrow does not exist.
    /// </summary>
    /// <param name="rowId">The row id to get.</param>
    /// <param name="subrowId">The subrow id to get.</param>
    /// <returns>A row object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the sheet does not have a row at that <paramref name="rowId"/>.</exception>
    public T GetSubrow( uint rowId, ushort subrowId )
    {
        ref readonly var lookup = ref RawSheet.GetRawRowOrNullRef( rowId, out var rowIndex );
        if( Unsafe.IsNullRef( in lookup ) )
            throw new ArgumentOutOfRangeException( nameof( rowId ), rowId, null );

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( subrowId, lookup.SubrowCount );

        return UnsafeCreateSubrow( rowIndex, subrowId, in lookup );
    }

    /// <summary>
    /// Gets the <paramref name="subrowId"/>th subrow of the <paramref name="rowIndex"/>th row in this sheet, ordered by row id in ascending order.
    /// </summary>
    /// <remarks>If you are looking to find a subrow by its id, use <see cref="GetSubrow(uint, ushort)"/> instead.</remarks>
    /// <param name="rowIndex">The zero-based index of this row.</param>
    /// <param name="subrowId">The subrow id to get.</param>
    /// <returns>A row object.</returns>
    public T GetSubrowAt( int rowIndex, ushort subrowId )
    {
        var offsetLookupTable = RawRows;
        ArgumentOutOfRangeException.ThrowIfNegative( rowIndex );
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( rowIndex, offsetLookupTable.Length );

        ref readonly var lookup = ref RawSheet.UnsafeGetRowLookupAt( rowIndex );
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( subrowId, lookup.SubrowCount );

        return UnsafeCreateSubrow( rowIndex, subrowId, in lookup );
    }

    /// <inheritdoc/>
    public bool Contains( SubrowCollection item ) => ReferenceEquals( item.Sheet.RawSheet, RawSheet ) && HasRow( item.RowId );

    /// <inheritdoc/>
    public void CopyTo( SubrowCollection[] array, int arrayIndex )
    {
        ArgumentNullException.ThrowIfNull( array );
        ArgumentOutOfRangeException.ThrowIfNegative( arrayIndex );
        if( Count > array.Length - arrayIndex )
            throw new ArgumentException( "The number of elements in the source list is greater than the available space." );

        var rowIndex = 0;
        foreach( var lookup in RawRows )
            array[ arrayIndex++ ] = new( this, in lookup, rowIndex++ );
    }

    void ICollection< SubrowCollection >.Add( SubrowCollection item ) => throw new NotSupportedException();

    void ICollection< SubrowCollection >.Clear() => throw new NotSupportedException();

    bool ICollection< SubrowCollection >.Remove( SubrowCollection item ) => throw new NotSupportedException();

    /// <summary>Gets an enumerator that enumerates over all subrows.</summary>
    /// <returns>A new enumerator.</returns>
    public FlatEnumerator Flatten() => new( this );

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new( this );

    IEnumerator< SubrowCollection > IEnumerable< SubrowCollection >.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public override string ToString() => $"{Name}<{typeof( T ).Name}>({Language}, {Variant}, {Count} row(s), {Columns.Count} column(s))";

    /// <summary>Creates a subrow at the given index, without checking for bounds or preconditions.</summary>
    /// <param name="rowIndex">Index of the desired row.</param>
    /// <param name="subrowId">Index of the desired subrow.</param>
    /// <returns>A new instance of <typeparamref name="T"/>.</returns>
    private T UnsafeCreateSubrowAt( int rowIndex, ushort subrowId ) =>
        UnsafeCreateSubrow( rowIndex, subrowId, in RawSheet.UnsafeGetRowLookupAt( rowIndex ) );

    /// <summary>Creates a subrow using the given lookup data, without checking for bounds or preconditions.</summary>
    /// <param name="rowIndex">Index of the desired row.</param>
    /// <param name="subrowId">Index of the desired subrow.</param>
    /// <param name="row">Lookup data for the desired row.</param>
    /// <returns>A new instance of <typeparamref name="T"/>.</returns>
    private T UnsafeCreateSubrow( int rowIndex, ushort subrowId, scoped ref readonly RawExcelRow row )
    {
        if( _rowCache is null )
            return T.Create( row with { SubrowId = subrowId } );

        ref var slots = ref Unsafe.Add( ref MemoryMarshal.GetArrayDataReference( _rowCache ), rowIndex );
        if( slots is null )
            Interlocked.CompareExchange( ref slots, new object?[row.SubrowCount], null );

        ref var slot = ref Unsafe.Add( ref MemoryMarshal.GetArrayDataReference( slots ), subrowId );
        if( slot is null )
            Interlocked.CompareExchange( ref slot, T.Create( row with { SubrowId = subrowId } ), null );

        return (T) slot;
    }
}