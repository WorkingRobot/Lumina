using System.Collections;
using System.Collections.Generic;
using Lumina.Data.Structs.Excel;

namespace Lumina.Excel.Sheets;

/// <summary>A typed Excel sheet of <see cref="ExcelVariant.Subrows"/> variant that wraps around a <see cref="RawSubrowExcelSheet"/>.</summary>
/// <typeparam name="T">Type of the rows contained within.</typeparam>
public readonly partial struct SubrowExcelSheet< T >
{
    /// <summary>Represents an enumerator that iterates over all rows in a <see cref="SubrowExcelSheet{T}"/>.</summary>
    /// <param name="sheet">The sheet to iterate over.</param>
    public struct Enumerator( SubrowExcelSheet< T > sheet ) : IEnumerator< SubrowCollection >
    {
        private int _index = -1;

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public SubrowCollection Current { get; private set; }

        readonly object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if( ++_index < sheet.Count )
            {
                // RawSheet.UnsafeGetRowLookupAt must be called only when the preconditions are validated.
                // If it is to be called on-demand from get_Current, then it may end up being called with invalid parameters,
                // so we create the instance in advance here.
                Current = new( sheet, in sheet.RawSheet.UnsafeGetRowLookupAt( _index ), _index );
                return true;
            }

            --_index;
            return false;
        }

        /// <inheritdoc/>
        public void Reset() =>
            _index = -1;

        /// <inheritdoc/>
        public readonly void Dispose()
        { }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof( Enumerator )}({_index}/{sheet.Count} for {sheet})";
    }

    /// <summary>Represents an enumerator that iterates over all subrows in a <see cref="SubrowExcelSheet{T}"/>.</summary>
    /// <param name="sheet">The sheet to iterate over.</param>
    public struct FlatEnumerator( SubrowExcelSheet< T > sheet ) : IEnumerator< T >, IEnumerable< T >
    {
        private int _index = -1;
        private ushort _subrowIndex = ushort.MaxValue;
        private ushort _subrowCount;

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public T Current { get; private set; }

        readonly object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if( ++_subrowIndex >= _subrowCount )
            {
                while( true )
                {
                    if( ++_index >= sheet.Count )
                    {
                        --_subrowIndex;
                        --_index;
                        return false;
                    }

                    _subrowCount = sheet.RawSheet.UnsafeGetRowLookupAt( _index ).SubrowCount;
                    if( _subrowCount == 0 )
                        continue;

                    _subrowIndex = 0;
                    break;
                }
            }

            // UnsafeCreateSubrowAt must be called only when the preconditions are validated.
            // If it is to be called on-demand from get_Current, then it may end up being called with invalid parameters,
            // so we create the instance in advance here.
            Current = sheet.UnsafeCreateSubrowAt( _index, _subrowIndex );
            return true;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _index = -1;
            _subrowIndex = ushort.MaxValue;
            _subrowCount = 0;
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        { }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public readonly FlatEnumerator GetEnumerator() => new( sheet );

        readonly IEnumerator< T > IEnumerable< T >.GetEnumerator() => GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public override string ToString() => $"{nameof( FlatEnumerator )}({_subrowIndex}/{_subrowCount}, {_index}/{sheet.Count} for {sheet})";
    }
}