using System.Collections;
using System.Collections.Generic;
using Lumina.Data.Structs.Excel;

namespace Lumina.Excel.Sheets;

/// <summary>A typed Excel sheet of <see cref="ExcelVariant.Default"/> variant that wraps around a <see cref="RawExcelSheet"/>.</summary>
/// <typeparam name="T">Type of the rows contained within.</typeparam>
public readonly partial struct ExcelSheet< T >
{
    /// <summary>Represents an enumerator that iterates over all rows in a <see cref="ExcelSheet{T}"/>.</summary>
    /// <param name="sheet">The sheet to iterate over.</param>
    public struct Enumerator( ExcelSheet< T > sheet ) : IEnumerator< T >
    {
        private int _index = -1;

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public T Current { get; private set; }

        readonly object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if( ++_index < sheet.Count )
            {
                // UnsafeCreateRowAt must be called only when the preconditions are validated.
                // If it is to be called on-demand from get_Current, then it may end up being called with invalid parameters,
                // so we create the instance in advance here.
                Current = sheet.UnsafeCreateRowAt( _index );
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

        /// <inheritdoc/>
        public override string ToString() => $"{nameof( Enumerator )}({_index}/{sheet.Count} for {sheet})";
    }
}