using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Lumina.Data.Structs.Excel;
using Lumina.Excel.Rows;

namespace Lumina.Excel.Sheets;

/// <summary>A typed Excel sheet of <see cref="ExcelVariant.Subrows"/> variant that wraps around a <see cref="RawSubrowExcelSheet"/>.</summary>
/// <typeparam name="T">Type of the rows contained within.</typeparam>
public readonly partial struct SubrowExcelSheet< T >
    where T : struct, IExcelRow< T >
{
    /// <summary>Collection of subrows under one row.</summary>
    public readonly struct SubrowCollection : IList< T >, IReadOnlyList< T >
    {
        private readonly RawExcelRow _rawRow;

        internal SubrowCollection( SubrowExcelSheet< T > sheet, scoped ref readonly RawExcelRow rawRow )
        {
            Sheet = sheet;
            _rawRow = rawRow;
        }

        /// <summary>Gets the associated sheet.</summary>
        public SubrowExcelSheet< T > Sheet { get; }

        /// <summary>Gets the Row ID of the subrows contained within.</summary>
        public uint RowId => _rawRow.RowId;

        /// <inheritdoc cref="ICollection{T}.Count"/>
        public int Count => _rawRow.SubrowCount;

        bool ICollection< T >.IsReadOnly => true;

        /// <inheritdoc/>
        public T this[ int index ] {
            [MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
            get {
                ArgumentOutOfRangeException.ThrowIfNegative( index );
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( index, Count );
                return Sheet.UnsafeCreateSubrow( in _rawRow, unchecked( (ushort) index ) );
            }
        }

        /// <inheritdoc/>
        T IList< T >.this[ int index ] {
            get => this[ index ];
            set => throw new NotSupportedException();
        }

        void IList< T >.Insert( int index, T item ) => throw new NotSupportedException();

        void IList< T >.RemoveAt( int index ) => throw new NotSupportedException();

        void ICollection< T >.Add( T item ) => throw new NotSupportedException();

        void ICollection< T >.Clear() => throw new NotSupportedException();

        bool ICollection< T >.Remove( T item ) => throw new NotSupportedException();

        /// <inheritdoc/>
        public int IndexOf( T item )
        {
            if( item.RawRow != _rawRow )
                return -1;

            var row = Sheet.UnsafeCreateSubrow( in _rawRow, item.RawRow.SubrowId );
            return EqualityComparer< T >.Default.Equals( item, row ) ? item.RawRow.SubrowId : -1;
        }

        /// <inheritdoc/>
        public bool Contains( T item ) => IndexOf( item ) != -1;

        /// <inheritdoc/>
        public void CopyTo( T[] array, int arrayIndex )
        {
            ArgumentNullException.ThrowIfNull( array );
            ArgumentOutOfRangeException.ThrowIfNegative( arrayIndex );
            if( Count > array.Length - arrayIndex )
                throw new ArgumentException( "The number of elements in the source list is greater than the available space." );
            for( var i = 0; i < Count; i++ )
                array[ arrayIndex++ ] = Sheet.UnsafeCreateSubrow( in _rawRow, unchecked( (ushort) i ) );
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public SubrowEnumerator GetEnumerator() => new( this );

        IEnumerator< T > IEnumerable< T >.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>Enumerator that enumerates over subrows under one row.</summary>
        /// <param name="subrowCollection">Subrow collection to iterate over.</param>
        public struct SubrowEnumerator( SubrowCollection subrowCollection ) : IEnumerator< T >
        {
            private int _index = -1;

            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public T Current { get; private set; }

            readonly object IEnumerator.Current => Current;

            /// <inheritdoc/>
            public bool MoveNext()
            {
                if( ++_index < subrowCollection.Count )
                {
                    // UnsafeCreateSubrow must be called only when the preconditions are validated.
                    // If it is to be called on-demand from get_Current, then it may end up being called with invalid parameters,
                    // so we create the instance in advance here.
                    Current = subrowCollection.Sheet.UnsafeCreateSubrow( in subrowCollection._rawRow, unchecked( (ushort) _index ) );
                    return true;
                }

                --_index;
                return false;
            }

            /// <inheritdoc/>
            public void Reset() => _index = -1;

            /// <inheritdoc/>
            public readonly void Dispose()
            { }
        }
    }
}