using System.Collections;
using System.Collections.Generic;

namespace Lumina.Excel;

/// <summary>
/// A collection helper used to layout and structure Excel rows.
/// </summary>
/// <remarks>Mostly an implementation detail for reading Excel rows. This type does not store or hold any row data, and is therefore lightweight and trivially constructable.</remarks>
/// <typeparam name="T">A type that wraps a group of fields inside a row.</typeparam>
public readonly partial struct Collection< T >
{
    /// <summary>Enumerator that enumerates over the different items.</summary>
    /// <param name="collection">Collection to iterate over.</param>
    public struct Enumerator( Collection< T > collection ) : IEnumerator< T >
    {
        private int _index = -1;

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public T Current { get; private set; }

        readonly object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if( ++_index < collection.Count )
            {
                // UnsafeCreateAt must be called only when the preconditions are validated.
                // If it is to be called on-demand from get_Current, then it may end up being called with invalid parameters,
                // so we create the instance in advance here.
                Current = collection.UnsafeCreateAt( _index );
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